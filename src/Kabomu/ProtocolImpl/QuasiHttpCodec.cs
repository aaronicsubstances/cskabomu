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
    /// The standard encoder and decoder of quasi http request and response
    /// headers in the Kabomu library. Also contains constants
    /// involved in implementing the quasi web protocol.
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
        /// Serializes quasi http request headers.
        /// </summary>
        /// <param name="reqHeaders">source of quasi http request headers</param>
        /// <param name="maxHeadersSize">limit on size of serialized result.
        /// Can be null or zero for a default value to be used.</param>
        /// <returns>serialized representation of quasi http request headeres</returns>
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

        /// <summary>
        /// Serializes quasi http response headers.
        /// </summary>
        /// <param name="resHeaders">source of quasi http response headers</param>
        /// <param name="maxHeadersSize">limit on size of serialized result.
        /// Can be null or zero for a default value to be used.</param>
        /// <returns>serialized representation of quasi http response headeres</returns>
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
                throw new QuasiHttpException("quasi http headers cannot " +
                    "contain newlines",
                    QuasiHttpException.ReasonCodeProtocolViolation);
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
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({effectiveByteCount} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            if (effectiveByteCount > HardLimitOnMaxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers too " +
                    $"large ({effectiveByteCount} > {HardLimitOnMaxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            return MiscUtils.StringToBytes(serialized);
        }

        /// <summary>
        /// Deserializes a quasi http request header seection from
        /// a list of byte chunks.
        /// </summary>
        /// <param name="byteChunks">list of byte chunks to deserialize</param>
        /// <param name="request">object whose header-related properties will be
        /// set with decoded quasi http request headers</param>
        /// <exception cref="QuasiHttpException">if byte chunks argument contains
        /// invalid quasi http request headers</exception>
        public static void DecodeRequestHeaders(List<byte[]> byteChunks,
            IQuasiHttpRequest request)
        {
            if (byteChunks == null)
            {
                throw new ArgumentNullException(nameof(byteChunks));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            IList<IList<string>> csv;
            try
            {
                csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                    MiscUtils.ConcatBuffers(byteChunks)));
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    "invalid encoded quasi http request headers",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            if (csv.Count < 2)
            {
                throw new QuasiHttpException(
                    "invalid encoded quasi http request headers",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            // skip first row.
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpException(
                    "invalid quasi http request line",
                    QuasiHttpException.ReasonCodeProtocolViolation);
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
                throw new QuasiHttpException(
                    "invalid quasi http request content length",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            request.Headers = DecodeRemainingHeaders(csv);
        }

        /// <summary>
        /// Deserializes a quasi http response header seection from
        /// a list of byte chunks.
        /// </summary>
        /// <param name="byteChunks">list of byte chunks to deserialize</param>
        /// <param name="response">object whose header-related properties will be
        /// set with decoded quasi http response headers</param>
        /// <exception cref="QuasiHttpException">if byte chunks argument contains
        /// invalid quasi http response headers</exception>
        public static void DecodeResponseHeaders(List<byte[]> byteChunks,
            IQuasiHttpResponse response)
        {
            if (byteChunks == null)
            {
                throw new ArgumentNullException(nameof(byteChunks));
            }
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            IList<IList<string>> csv;
            try
            {
                csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                    MiscUtils.ConcatBuffers(byteChunks)));
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    "invalid encoded quasi http response headers",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            if (csv.Count < 2)
            {
                throw new QuasiHttpException(
                    "invalid encoded quasi http response headers",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            // skip version row.
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpException(
                    "invalid quasi http response status line",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            try
            {
                response.StatusCode = MiscUtils.ParseInt32(specialHeader[0]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    "invalid quasi http response status code",
                    QuasiHttpException.ReasonCodeProtocolViolation,
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
                throw new QuasiHttpException(
                    "invalid quasi http response content length",
                    QuasiHttpException.ReasonCodeProtocolViolation,
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

        /// <summary>
        /// Reads as many 512-byte chunks as needed to detect
        /// the portion of a source stream representing a quasi
        /// http request or response header section.
        /// </summary>
        /// <param name="inputStream">source stream</param>
        /// <param name="encodedHeadersReceiver">list which
        /// will receive all the byte chunks to be read.</param>
        /// <param name="maxHeadersSize">limit on total
        /// size of byte chunks to be read. Can be zero in order
        /// for a default value to be used.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>a task representing the asynchronous operation</returns>
        /// <exception cref="QuasiHttpException">Limit on
        /// total size of byte chunks has been reached and still
        /// end of header section has not been determined</exception>
        public static async Task ReadEncodedHeaders(Stream inputStream,
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
                    throw new QuasiHttpException(
                        "size of quasi http headers to read exceed " +
                        $"max size ({totalBytesRead} > {maxHeadersSize})",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                var chunk = new byte[HeaderChunkSize];
                await MiscUtils.ReadBytesFully(inputStream, chunk,
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
    }
}