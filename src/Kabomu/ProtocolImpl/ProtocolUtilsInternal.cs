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

        public static async Task WriteEntityToTransport(bool isResponse,
            object entity, Stream writableStream,
            IQuasiHttpConnection connection)
        {
            if (writableStream == null)
            {
                throw new MissingDependencyException(
                    "no writable stream found for transport");
            }
            long contentLength;
            Stream body;
            byte[] encodedHeaders;
            if (isResponse)
            {
                var response = (IQuasiHttpResponse)entity;
                contentLength = response.ContentLength;
                body = response.Body;
                encodedHeaders = QuasiHttpCodec.EncodeResponseHeaders(response,
                    connection.ProcessingOptions?.MaxHeadersSize);
            }
            else
            {
                var request = (IQuasiHttpRequest)entity;
                contentLength = request.ContentLength;
                body = request.Body;
                encodedHeaders = QuasiHttpCodec.EncodeRequestHeaders(request,
                    connection.ProcessingOptions?.MaxHeadersSize);
            }
            await TlvUtils.WriteTlv(writableStream,
                QuasiHttpCodec.TagForHeaders, encodedHeaders,
                connection.CancellationToken);
            if (contentLength == 0)
            {
                return;
            }
            if (body == null)
            {
                var errMsg = isResponse ? "no response body" :
                    "no request body";
                throw new QuasiHttpException(errMsg);
            }
            if (contentLength < 0)
            {
                body = new BodyChunkEncodingStreamInternal(body);
            }
            // don't enforce positive content lengths when writing out
            // quasi http bodies
            await body.CopyToAsync(writableStream, connection.CancellationToken);
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
            var encodedHeaders = await ReadEncodedHeaders(
                readableStream,
                connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                connection.CancellationToken);

            if (isResponse)
            {
                var response = new DefaultQuasiHttpResponse();
                QuasiHttpCodec.DecodeResponseHeaders(encodedHeaders, 0,
                    encodedHeaders.Length, response);
                if (response.ContentLength != 0)
                {
                    if (response.ContentLength > 0)
                    {
                        response.Body = new ContentLengthEnforcingStreamInternal(
                            readableStream, response.ContentLength);
                    }
                    else
                    {
                        response.Body = new BodyChunkDecodingStreamInternal(
                            readableStream);
                    }
                }
                return response;
            }
            else
            {
                var request = new DefaultQuasiHttpRequest
                {
                    Environment = connection.Environment
                };
                QuasiHttpCodec.DecodeRequestHeaders(encodedHeaders, 0,
                    encodedHeaders.Length, request);
                if (request.ContentLength != 0)
                {
                    if (request.ContentLength > 0)
                    {
                        request.Body = new ContentLengthEnforcingStreamInternal(
                            readableStream, request.ContentLength);
                    }
                    else
                    {
                        request.Body = new BodyChunkDecodingStreamInternal(
                            readableStream);
                    }
                }
                return request;
            }
        }

        /// <summary>
        /// Reads the portion of a source stream representing a quasi
        /// http request or response header section.
        /// </summary>
        /// <param name="inputStream">source stream</param>
        /// <param name="maxHeadersSize">limit on total
        /// size of byte chunks to be read. Can be zero in order
        /// for a default value to be used.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>a task whose result will be the enocode headers</returns>
        /// <exception cref="QuasiHttpException">Limit on
        /// total size of byte chunks has been reached and still
        /// end of header section has not been determined</exception>
        public static async Task<byte[]> ReadEncodedHeaders(Stream inputStream,
            int maxHeadersSize = 0,
            CancellationToken cancellationToken = default)
        {
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }
            var headersSize = await TlvUtils.ReadTagAndLengthOnly(
                inputStream, QuasiHttpCodec.TagForHeaders, cancellationToken);
            if (headersSize > maxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({headersSize} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            var encodedHeaders = new byte[maxHeadersSize];
            await IOUtilsInternal.ReadBytesFully(inputStream,
                encodedHeaders, 0, encodedHeaders.Length,
                cancellationToken);
            return encodedHeaders;
        }

        public static async Task<Stream> BufferResponseBody(
            long contentLength, Stream body,
            int? bufferingSizeLimit,
            CancellationToken cancellationToken)
        {
            if (bufferingSizeLimit == null || bufferingSizeLimit.Value <= 0)
            {
                bufferingSizeLimit = IOUtilsInternal.DefaultDataBufferLimit;
            }
            if (contentLength < 0)
            {
                var buffered = new MemoryStream();
                bool success = await IOUtilsInternal.CopyBytesUpToGivenLimit(body,
                    buffered, bufferingSizeLimit.Value, cancellationToken);
                if (!success)
                {
                    throw new QuasiHttpException(
                        "response body of indeterminate length exceeds buffering limit of " +
                        $"{bufferingSizeLimit} bytes",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                buffered.Position = 0; // reset for reading.
                return buffered;
            }
            else
            {
                if (contentLength > bufferingSizeLimit.Value)
                {
                    throw new QuasiHttpException(
                        "response body length exceeds buffering limit " +
                        $"({contentLength} > {bufferingSizeLimit})",
                        QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
                }
                var buffer = new byte[(int)contentLength];
                await IOUtilsInternal.ReadBytesFully(body,
                    buffer, 0, buffer.Length,
                    cancellationToken);
                return new MemoryStream(buffer);
            }
        }
    }
}
