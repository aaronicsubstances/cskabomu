using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// Contains constants and helper functions involved in implementing quasi
    /// web protocol.
    /// </summary>
    public static class QuasiHttpCodec
    {
        /// <summary>
        /// Request environment variable for local server endpoint.
        /// </summary>
        public static readonly string EnvKeyLocalPeerEndpoint = "kabomu.local_peer_endpoint";

        /// <summary>
        /// Request environment variable for remote client endpoint.
        /// </summary>
        public static readonly string EnvKeyRemotePeerEndpoint = "kabomu.remote_peer_endpoint";

        /// <summary>
        /// Request environment variable for the transport instance from which a request was received.
        /// </summary>
        public static readonly string EnvKeyTransportInstance = "kabomu.transport";

        /// <summary>
        /// Request environment variable for the connection from which a request was received.
        /// </summary>
        public static readonly string EnvKeyConnection = "kabomu.connection";

        /// <summary>
        /// Environment variable for indicating that a request or response
        /// should not be sent at all. Intended
        /// for use in responding to fire and forget requests, as well as
        /// cases where request or response has been sent already by other
        /// means.
        /// </summary>
        public static readonly string EnvKeySkipSending = "kabomu.skip_sending";

        /// <summary>
        /// Environment variable indicating that the response body 
        /// received from transport should be returned to client without
        /// any decoding applied.
        /// </summary>
        public static readonly string EnvKeySkipResBodyDecoding = "kabomu.skip_res_body_decoding";

        /// <summary>
        /// Equals HTTP method "GET".
        /// </summary>
        public static readonly string MethodGet = "GET";

        /// <summary>
        /// Equals HTTP method "POST".
        /// </summary>
        public static readonly string MethodPost = "POST";

        /// <summary>
        /// Equals HTTP method "PUT".
        /// </summary>
        public static readonly string MethodPut = "PUT";

        /// <summary>
        /// Equals HTTP method "DELETE".
        /// </summary>
        public static readonly string MethodDelete = "DELETE";

        /// <summary>
        /// Equals HTTP method "HEAD".
        /// </summary>
        public static readonly string MethodHead = "HEAD";

        /// <summary>
        /// Equals HTTP method "OPTIONS".
        /// </summary>
        public static readonly string MethodOptions = "OPTIONS";

        /// <summary>
        /// Equals HTTP method "PATCH".
        /// </summary>
        public static readonly string MethodPatch = "PATCH";

        /// <summary>
        /// Equals HTTP method "TRACE".
        /// </summary>
        public static readonly string MethodTrace = "TRACE";

        /// <summary>
        /// Equals HTTP method "CONNECT".
        /// </summary>
        public static readonly string MethodConnect = "CONNECT";

        /// <summary>
        /// Status code value of 200, equivalent to HTTP status code 200 OK.
        /// </summary>
        public static readonly int StatusCodeOk = 200;

        /// <summary>
        /// Status code value of 400, equivalent to HTTP status code 400 Bad Request.
        /// </summary>
        public static readonly int StatusCodeClientErrorBadRequest = 400;

        /// <summary>
        /// Status code value of 401, equivalent to HTTP status code 401 Unauthorized.
        /// </summary>
        public static readonly int StatusCodeClientErrorUnauthorized = 401;

        /// <summary>
        /// Status code value of 403, equivalent to HTTP status code 403 Forbidden.
        /// </summary>
        public static readonly int StatusCodeClientErrorForbidden = 403;

        /// <summary>
        /// Status code value of 404, equivalent to HTTP status code 404 Not Found.
        /// </summary>
        public static readonly int StatusCodeClientErrorNotFound = 404;

        /// <summary>
        /// Status code value of 405, equivalent to HTTP status code 405 Method Not Allowed.
        /// </summary>
        public static readonly int StatusCodeClientErrorMethodNotAllowed = 405;

        /// <summary>
        /// Status code value of 413, equivalent to HTTP status code 413 Payload Too Large.
        /// </summary>
        public static readonly int StatusCodeClientErrorPayloadTooLarge = 413;

        /// <summary>
        /// Status code value of 414, equivalent to HTTP status code 414 URI Too Long.
        /// </summary>
        public static readonly int StatusCodeClientErrorURITooLong = 414;

        /// <summary>
        /// Status code value of 415, equivalent to HTTP status code 415 Unsupported Media Type.
        /// </summary>
        public static readonly int StatusCodeClientErrorUnsupportedMediaType = 415;

        /// <summary>
        /// Status code value of 422, equivalent to HTTP status code 422 Unprocessable Entity.
        /// </summary>
        public static readonly int StatusCodeClientErrorUnprocessableEntity = 422;

        /// <summary>
        /// Status code value of 429, equivalent to HTTP status code 429 Too Many Requests.
        /// </summary>
        public static readonly int StatusCodeClientErrorTooManyRequests = 429;

        /// <summary>
        /// Status code value of 500, equivalent to HTTP status code 500 Internal Server Error.
        /// </summary>
        public static readonly int StatusCodeServerError = 500;

        /// <summary>
        /// The default value of maximum size of headers in a request or response.
        /// </summary>
        public const int DefaultMaxHeadersSize = 8_192;

        /// <summary>
        /// The maximum possible size that headers in a request or response
        /// cannot exceed.
        /// </summary>
        private const int HardLimitOnMaxHeadersSize = 999_999;

        /// <summary>
        /// This field gives a number of which all header sizes are
        /// an integral multiple of.
        /// </summary>
        private const int HeaderChunkSize = 512;

        /// <summary>
        /// First version of quasi web protocol.
        /// </summary>
        public const string ProtocolVersion01 = "01";

        /// <summary>
        /// The limit of data buffering when reading byte streams into memory. Equal to 128 MB.
        /// </summary>
        public static readonly int DefaultDataBufferLimit = 134_217_728;

        public static byte[] EncodeRequestHeaders(
            IQuasiHttpRequest reqHeaders,
            int? maxHeadersSize = null)
        {
            var uniqueRow = new List<string>
            {
                reqHeaders.HttpMethod ?? "",
                reqHeaders.Target ?? "",
                reqHeaders.HttpVersion ?? "",
                reqHeaders.ContentLength.ToString()
            };
            return EncodeRemainingHeaders(uniqueRow,
                reqHeaders.Headers, maxHeadersSize ?? 0);
        }

        public static byte[] EncodeResponseHeaders(
            IQuasiHttpResponse resHeaders,
            int? maxHeadersSize = null)
        {
            var uniqueRow = new List<string>
            {
                resHeaders.StatusCode.ToString(),
                resHeaders.HttpStatusMessage ?? "",
                resHeaders.HttpVersion ?? "",
                resHeaders.ContentLength.ToString()
            };
            return EncodeRemainingHeaders(uniqueRow,
                resHeaders.Headers, maxHeadersSize ?? 0);
        }

        private static byte[] EncodeRemainingHeaders(List<string> uniqueRow, IDictionary<string, IList<string>> headers,
            int maxHeadersSize)
        {
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = DefaultMaxHeadersSize;
            }
            var csv = new List<IList<string>>();
            csv.Add(new List<string>
            {
                ProtocolVersion01
            });
            csv.Add(uniqueRow);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Value == null || header.Value.Count == 0)
                    {
                        continue;
                    }
                    var headerRow = new List<string> { header.Key ?? "" };
                    foreach (var v in header.Value)
                    {
                        headerRow.Add(v ?? "");
                    }
                    csv.Add(headerRow);
                }
            }

            // ensure there are no new lines in csv items
            if (csv.Any(row => row.Any(item => item.Contains('\n') ||
                item.Contains('\r'))))
            {
                throw new QuasiHttpRequestProcessingException("quasi http headers cannot " +
                    "contain newlines",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation);
            }

            // add at least two line feeds to ensure byte count
            // is multiple of header chunk size.
            var serialized = CsvUtils.Serialize(csv);
            var effectiveByteCount = MiscUtils.GetByteCount(serialized);
            var lfCount = (int)Math.Ceiling(effectiveByteCount /
                (double)HeaderChunkSize) * HeaderChunkSize -
                effectiveByteCount;
            if (lfCount < 2)
            {
                lfCount += HeaderChunkSize;
            }
            serialized += "".PadRight(lfCount, '\n');
            effectiveByteCount += lfCount;

            // finally check that byte count of csv doesn't exceed limit.
            if (effectiveByteCount > maxHeadersSize)
            {
                throw new QuasiHttpRequestProcessingException("quasi http headers exceed " +
                    $"max size ({effectiveByteCount} > {maxHeadersSize})",
                    QuasiHttpRequestProcessingException.ReasonCodeMessageLengthLimitExceeded);
            }
            if (effectiveByteCount > HardLimitOnMaxHeadersSize)
            {
                throw new QuasiHttpRequestProcessingException("quasi http headers too " +
                    $"large ({effectiveByteCount} > {HardLimitOnMaxHeadersSize})",
                    QuasiHttpRequestProcessingException.ReasonCodeMessageLengthLimitExceeded);
            }
            return MiscUtils.StringToBytes(serialized);
        }

        public static void DecodeRequestHeaders(List<byte[]> encodedCsv,
            IQuasiHttpRequest request)
        {
            var csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                MiscUtils.ConcatBuffers(encodedCsv)));
            if (csv.Count < 2)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid encoded quasi http request headers",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation);
            }
            // skip first row.
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid quasi http request line",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation);
            }
            request.HttpMethod = specialHeader[0];
            request.Target = specialHeader[1];
            request.HttpVersion = specialHeader[2];
            try
            {
                request.ContentLength = MiscUtils.ParseInt48(
                    specialHeader[3]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid quasi http request content length",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation,
                    e);
            }
            request.Headers = DecodeRemainingHeaders(csv);
        }

        public static void DecodeResponseHeaders(List<byte[]> encodedCsv,
            IQuasiHttpResponse response)
        {
            var csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                MiscUtils.ConcatBuffers(encodedCsv)));
            if (csv.Count < 2)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid encoded quasi http response headers",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation);
            }
            // skip version row.
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid quasi http response status line",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation);
            }
            try
            {
                response.StatusCode = MiscUtils.ParseInt32(specialHeader[0]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid quasi http response status code",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation,
                    e);
            }
            response.HttpStatusMessage = specialHeader[1];
            response.HttpVersion = specialHeader[2];
            try
            {
                response.ContentLength = MiscUtils.ParseInt48(specialHeader[3]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpRequestProcessingException(
                    "invalid quasi http response content length",
                    QuasiHttpRequestProcessingException.ReasonCodeProtocolViolation,
                    e);
            }
            response.Headers = DecodeRemainingHeaders(csv);
        }

        private static IDictionary<string, IList<string>> DecodeRemainingHeaders(
            IList<IList<string>> csv)
        {
            var headers = new Dictionary<string, IList<string>>();
            for (int i = 2; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                // merge headers with the same name in different rows.
                string headerName = headerRow[0];
                if (!headers.ContainsKey(headerName))
                {
                    headers.Add(headerName, new List<string>());
                }
                var headerValues = headers[headerName];
                foreach (var headerValue in headerRow.Skip(1))
                {
                    headerValues.Add(headerValue);
                }
            }
            return headers;
        }

        public static async Task ReadEncodedHeaders(Stream source,
            List<byte[]> encodedHeadersReceiver, int maxHeadersSize,
            CancellationToken cancellationToken = default)
        {
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = DefaultMaxHeadersSize;
            }
            int totalBytesRead = 0;
            bool previousChunkEndsWithLf = false;
            while (true)
            {
                totalBytesRead += HeaderChunkSize;
                if (totalBytesRead > maxHeadersSize)
                {
                    throw new QuasiHttpRequestProcessingException(
                        "size of quasi http headers to read exceed " +
                        $"max size ({totalBytesRead} > {maxHeadersSize})",
                        QuasiHttpRequestProcessingException.ReasonCodeMessageLengthLimitExceeded);
                }
                var chunk = new byte[HeaderChunkSize];
                await MiscUtils.ReadBytesFully(source, chunk,
                    0, chunk.Length, cancellationToken);
                encodedHeadersReceiver.Add(chunk);
                byte newline = (byte)'\n';
                if (previousChunkEndsWithLf && chunk[0] == newline)
                {
                    // done.
                    break;
                }
                for (int i = 1; i < chunk.Length; i++)
                {
                    if (chunk[i] != newline)
                    {
                        continue;
                    }
                    if (chunk[i - 1] == newline)
                    {
                        // done.
                        // don't just break, as this will only quit
                        // the for loop and leave us in while loop.
                        return;
                    }
                }
                previousChunkEndsWithLf = chunk[^1] == newline;
            }
        }

        public static async Task<Stream> ReadAllBytes(Stream body,
            int bufferingLimit, CancellationToken cancellationToken)
        {
            var bufferingStream = new MemoryStream();

            var readBuffer = MiscUtils.AllocateReadBuffer();
            int totalBytesRead = 0;

            while (true)
            {
                int bytesToRead = Math.Min(readBuffer.Length, bufferingLimit - totalBytesRead);
                // force a read of 1 byte if there are no more bytes to read into memory stream buffer
                // but still remember that no bytes was expected.
                var expectedEndOfRead = false;
                if (bytesToRead == 0)
                {
                    bytesToRead = 1;
                    expectedEndOfRead = true;
                }
                int bytesRead = await body.ReadAsync(readBuffer, 0, bytesToRead,
                    cancellationToken);
                if (bytesRead > bytesToRead)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {bytesToRead})");
                }
                if (bytesRead > 0)
                {
                    if (expectedEndOfRead)
                    {
                        throw new QuasiHttpRequestProcessingException(
                            "response body of indeterminate length exceeds buffering limit of " +
                            $"{bufferingLimit} bytes",
                            QuasiHttpRequestProcessingException.ReasonCodeMessageLengthLimitExceeded);
                    }
                    bufferingStream.Write(readBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }
                else
                {
                    break;
                }
            }
            return bufferingStream;
        }
    }
}