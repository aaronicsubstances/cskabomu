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
    internal static class ProtocolUtilsInternal
    {
        public static bool? GetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key)
        {
            if (environment != null && environment.ContainsKey(key))
            {
                var value = environment[key];
                if (value is bool b)
                {
                    return b;
                }
                else if (value != null)
                {
                    return bool.Parse((string)value);
                }
            }
            return null;
        }

        public static async Task WrapTimeoutTask(Task<bool> timeoutTask,
            string timeoutMsg)
        {
            if (timeoutTask == null)
            {
                return;
            }
            if (await timeoutTask)
            {
                throw new QuasiHttpException(timeoutMsg,
                    QuasiHttpException.ReasonCodeTimeout);
            }
        }

        public static void ValidateHttpHeaderSection(bool isResponse,
            IList<IList<string>> csv)
        {
            if (csv.Count == 0)
            {
                throw new ExpectationViolationException(
                    "expected csv to contain at least the special header");
            }
            var specialHeader = csv[0];
            if (specialHeader.Count != 4)
            {
                throw new ExpectationViolationException(
                    "expected special header to have 4 values " +
                    $"instead of {specialHeader.Count}");
            }
            for (int i = 0; i < specialHeader.Count; i++)
            {
                var item = specialHeader[i];
                if (!ContainsOnlyPrintableAsciiChars(item, i == 2))
                {
                    throw new QuasiHttpException(
                        $"quasi http {(isResponse ? "status" : "request")} line " +
                        "field contains spaces, newlines or " +
                        "non-printable ASCII characters: " +
                        item,
                        QuasiHttpException.ReasonCodeProtocolViolation);
                }
            }
            for (int i = 1; i < csv.Count; i++)
            {
                var row = csv[i];
                if (row.Count < 2)
                {
                    throw new ExpectationViolationException(
                        "expected row to have at least 2 values " +
                        $"instead of {row.Count}");
                }
                var headerName = row[0];
                if (!ContainsOnlyHeaderNameChars(headerName))
                {
                    throw new QuasiHttpException(
                        "quasi http header name contains characters " +
                        "other than hyphen and English alphabets: " +
                        headerName,
                        QuasiHttpException.ReasonCodeProtocolViolation);
                }
                for (int j = 1; j < row.Count; j++)
                {
                    var headerValue = row[j];
                    if (!ContainsOnlyPrintableAsciiChars(headerValue, true))
                    {
                        throw new QuasiHttpException(
                            "quasi http header value contains newlines or " +
                            "non-printable ASCII characters: " + headerValue,
                            QuasiHttpException.ReasonCodeProtocolViolation);
                    }
                }
            }
        }

        public static bool ContainsOnlyHeaderNameChars(string v)
        {
            foreach (var c in v)
            {
                if (c >= '0' && c <= '9')
                {
                    // digits
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    // upper case
                }
                else if (c >= 'a' && c <= 'z')
                {
                    // lower case
                }
                else if (c == '-')
                {
                    // hyphen
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static bool ContainsOnlyPrintableAsciiChars(string v,
            bool allowSpace)
        {
            foreach (var c in v)
            {
                if (c < ' ' || c > 126)
                {
                    return false;
                }
                if (!allowSpace && c == ' ')
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Serializes quasi http request or response headers.
        /// </summary>
        /// <param name="reqOrStatusLine">request or response status line</param>
        /// <param name="remainingHeaders">headers after request or status line</param>
        /// <returns>serialized representation of quasi http headers</returns>
        public static byte[] EncodeQuasiHttpHeaders(bool isResponse,
            IList<string> reqOrStatusLine,
            IDictionary<string, IList<string>> remainingHeaders)
        {
            if (reqOrStatusLine == null)
            {
                throw new ArgumentNullException(nameof(reqOrStatusLine));
            }
            var csv = new List<IList<string>>();
            var specialHeader = new List<string>();
            foreach (var v in reqOrStatusLine)
            {
                specialHeader.Add(v ?? "");
            }
            csv.Add(specialHeader);
            if (remainingHeaders != null)
            {
                foreach (var header in remainingHeaders)
                {
                    if (string.IsNullOrEmpty(header.Key))
                    {
                        throw new QuasiHttpException(
                            "quasi http header name cannot be empty",
                            QuasiHttpException.ReasonCodeProtocolViolation);
                    }
                    if (header.Value == null || header.Value.Count == 0)
                    {
                        continue;
                    }
                    var headerRow = new List<string> { header.Key };
                    foreach (var v in header.Value)
                    {
                        if (string.IsNullOrEmpty(v))
                        {
                            throw new QuasiHttpException(
                                "quasi http header value cannot be empty",
                                QuasiHttpException.ReasonCodeProtocolViolation);
                        }
                        headerRow.Add(v);
                    }
                    csv.Add(headerRow);
                }
            }

            ValidateHttpHeaderSection(isResponse, csv);

            var serialized = MiscUtilsInternal.StringToBytes(
                CsvUtils.Serialize(csv));

            return serialized;
        }

        /// <summary>
        /// Deserializes a quasi http request or response header section.
        /// </summary>
        /// <param name="data">source of data to deserialize</param>
        /// <param name="offset">starting position in buffer to start
        /// deserializing from</param>
        /// <param name="length">number of bytes to deserialize</param>
        /// <param name="headersReceiver">will be extended with remaining headers found
        /// after the request or response line</param>
        /// <returns>request or response line, ie first row before headers</returns>
        /// <exception cref="QuasiHttpException">if byte slice argument contains
        /// invalid quasi http request or response headers</exception>
        public static IList<string> DecodeQuasiHttpHeaders(bool isResponse,
            byte[] data, int offset, int length,
            IDictionary<string, IList<string>> headersReceiver)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (!MiscUtilsInternal.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid byte buffer slice");
            }
            IList<IList<string>> csv;
            try
            {
                csv = CsvUtils.Deserialize(MiscUtilsInternal.BytesToString(
                    data, offset, length));
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http headers",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            if (csv.Count == 0)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http headers",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            var specialHeader = csv[0];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http {(isResponse ? "status" : "request")} line",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }

            // merge headers with the same normalized name in different rows.
            for (int i = 1; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                string headerName = headerRow[0].ToLowerInvariant();
                if (!headersReceiver.ContainsKey(headerName))
                {
                    headersReceiver.Add(headerName, new List<string>());
                }
                var headerValues = headersReceiver[headerName];
                foreach (var headerValue in headerRow.Skip(1))
                {
                    headerValues.Add(headerValue);
                }
            }

            return specialHeader;
        }

        /// <summary>
        /// Serializes quasi http request or response header section to a writable
        /// stream.
        /// </summary>
        /// <param name="dest">writable stream to which serialized quasi http request or
        /// response section will be written</param>
        /// <param name="reqOrStatusLine">request or response status line</param>
        /// <param name="remainingHeaders">headers after request or status line</param>
        /// <param name="maxHeadersSize">limit on size of serialized result.
        /// Can be zero for a default value to be used.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>task represent asynchronous operation</returns>
        /// <exception cref="QuasiHttpException">if serialized is too large</exception>
        public static async Task WriteQuasiHttpHeaders(
            bool isResponse,
            Stream dest,
            IList<string> reqOrStatusLine,
            IDictionary<string, IList<string>> remainingHeaders,
            int maxHeadersSize = 0,
            CancellationToken cancellationToken = default)
        {
            var encodedHeaders = EncodeQuasiHttpHeaders(isResponse,
                reqOrStatusLine, remainingHeaders);
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }

            // finally check that byte count of csv doesn't exceed limit.
            if (encodedHeaders.Length > maxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({encodedHeaders.Length} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            var tagAndLen = new byte[8];
            TlvUtils.EncodeTag(TlvUtils.TagForQuasiHttpHeaders, tagAndLen, 0);
            TlvUtils.EncodeLength(encodedHeaders.Length, tagAndLen, 4);
            await dest.WriteAsync(tagAndLen, cancellationToken);
            await dest.WriteAsync(encodedHeaders, cancellationToken);
        }

        public static async Task<IList<string>> ReadQuasiHttpHeaders(
            bool isResponse,
            Stream src,
            IDictionary<string, IList<string>> headersReceiver,
            int maxHeadersSize = 0,
            CancellationToken cancellationToken = default)
        {
            var tagOrLen = new byte[4];
            await IOUtilsInternal.ReadBytesFully(src, tagOrLen, 0,
                tagOrLen.Length, cancellationToken);
            int tag = TlvUtils.DecodeTag(tagOrLen, 0);
            if (tag != TlvUtils.TagForQuasiHttpHeaders)
            {
                throw new QuasiHttpException(
                    $"unexpected quasi http headers tag: {tag}",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            await IOUtilsInternal.ReadBytesFully(src, tagOrLen, 0,
                tagOrLen.Length, cancellationToken);
            int headersSize = TlvUtils.DecodeLength(tagOrLen, 0);
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }
            if (headersSize > maxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({headersSize} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            var encodedHeaders = new byte[headersSize];
            await IOUtilsInternal.ReadBytesFully(src,
                encodedHeaders, 0, encodedHeaders.Length,
                cancellationToken);
            return DecodeQuasiHttpHeaders(isResponse,
                encodedHeaders, 0, encodedHeaders.Length,
                headersReceiver);
        }

        public static async Task WriteEntityToTransport(bool isResponse,
            object entity, Stream writableStream,
            IQuasiHttpConnection connection)
        {
            if (writableStream == null)
            {
                throw new MissingDependencyException(
                    "no writable stream found for transport");
            }
            Stream body;
            long contentLength;
            IList<string> reqOrStatusLine;
            IDictionary<string, IList<string>> headers;
            if (isResponse)
            {
                var response = (IQuasiHttpResponse)entity;
                headers = response.Headers;
                body = response.Body;
                contentLength = response.ContentLength;
                reqOrStatusLine = new string[] {
                    response.HttpVersion,
                    response.StatusCode.ToString(),
                    response.HttpStatusMessage,
                    null
                };
            }
            else
            {
                var request = (IQuasiHttpRequest)entity;
                headers = request.Headers;
                body = request.Body;
                contentLength = request.ContentLength;
                reqOrStatusLine = new string[] {
                    request.HttpMethod,
                    request.Target,
                    request.HttpVersion,
                    null
                };
            }
            // treat content lengths totally separate from body.
            // This caters for the HEAD method
            // which can be used to return a content length without a body
            // to download.
            reqOrStatusLine[3] = contentLength.ToString();
            await WriteQuasiHttpHeaders(isResponse, writableStream,
                reqOrStatusLine, headers,
                connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                connection.CancellationToken);
            if (body == null)
            {
                // don't proceed, even if content length is not zero.
                return;
            }
            if (contentLength > 0)
            {
                // don't enforce positive content lengths when writing out
                // quasi http bodies
                await body.CopyToAsync(writableStream,
                    connection.CancellationToken);
            }
            else
            {
                // proceed, even if content length is 0.
                var bodyWriter = TlvUtils.CreateTlvEncodingWritableStream(
                    writableStream, TlvUtils.TagForQuasiHttpBodyChunk);
                await body.CopyToAsync(bodyWriter, connection.CancellationToken);
                // write end of stream
                await bodyWriter.WriteAsync(null, 0, -1,
                    connection.CancellationToken);
            }
        }

        public static async Task<object> ReadEntityFromTransport(
            bool isResponse, Stream readableStream,
            IQuasiHttpConnection connection)
        {
            if (readableStream == null)
            {
                throw new MissingDependencyException(
                    "no readable stream found for transport");
            }
            var headersReceiver = new Dictionary<string, IList<string>>();
            var reqOrStatusLine = await ReadQuasiHttpHeaders(
                isResponse,
                readableStream,
                headersReceiver,
                connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                connection.CancellationToken);

            long contentLength;
            try
            {
                contentLength = MiscUtilsInternal.ParseInt48(
                    reqOrStatusLine[3]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http {(isResponse ? "response" : "request")} content length",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            Stream body = null;
            if (contentLength != 0)
            {
                if (contentLength > 0)
                {
                    body = TlvUtils.CreateContentLengthEnforcingStream(
                        readableStream, contentLength);
                }
                else
                {
                    body = TlvUtils.CreateTlvDecodingReadableStream(readableStream,
                        TlvUtils.TagForQuasiHttpBodyChunk,
                        TlvUtils.TagForQuasiHttpBodyChunkExt);
                }
            }
            if (isResponse)
            {
                var response = new DefaultQuasiHttpResponse();
                response.HttpVersion = reqOrStatusLine[0];
                try
                {
                    response.StatusCode = MiscUtilsInternal.ParseInt32(
                        reqOrStatusLine[1]);
                }
                catch (Exception e)
                {
                    throw new QuasiHttpException(
                        "invalid quasi http response status code",
                        QuasiHttpException.ReasonCodeProtocolViolation,
                        e);
                }
                response.HttpStatusMessage = reqOrStatusLine[2];
                response.Headers = headersReceiver;
                if (body != null)
                {
                    var bodySizeLimit = connection.ProcessingOptions?.
                        MaxResponseBodySize ?? 0;
                    if (bodySizeLimit >= 0)
                    {
                        body = TlvUtils.CreateMaxLengthEnforcingStream(body,
                            bodySizeLimit);
                    }
                    // can't implement response buffering, because of
                    // the HEAD method, with which a content length may
                    // be given but without a body to download.
                }
                response.Body = body;
                return response;
            }
            else
            {
                var request = new DefaultQuasiHttpRequest
                {
                    Environment = connection.Environment
                };
                request.HttpMethod = reqOrStatusLine[0];
                request.Target = reqOrStatusLine[1];
                request.HttpVersion = reqOrStatusLine[2];
                request.Headers = headersReceiver;
                request.Body = body;
                return request;
            }
        }
    }
}
