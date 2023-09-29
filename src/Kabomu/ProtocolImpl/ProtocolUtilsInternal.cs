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
            Stream body;
            if (isResponse)
            {
                var response = (IQuasiHttpResponse)entity;
                var statusLine = new object[] {
                    response.StatusCode, response.HttpStatusMessage, response.HttpVersion };
                await QuasiHttpCodec.WriteQuasiHttpHeaders(writableStream,
                    statusLine, response.Headers,
                    connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                    connection.CancellationToken);
                body = response.Body;
            }
            else
            {
                var request = (IQuasiHttpRequest)entity;
                var requestLine = new object[] {
                    request.HttpMethod, request.Target, request.HttpVersion };
                await QuasiHttpCodec.WriteQuasiHttpHeaders(writableStream,
                    requestLine, request.Headers,
                    connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                    connection.CancellationToken);
                body = request.Body;
            }
            var bodyWriter = new BodyChunkEncodingStreamInternal(
                writableStream);
            if (body != null)
            {
                await body.CopyToAsync(bodyWriter, connection.CancellationToken);
            }
            await bodyWriter.WriteTerminatingChunk(connection.CancellationToken);
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
            var reqOrStatusLineReceiver = new List<object>();
            var headersReceiver = new Dictionary<string, IList<string>>();
            await QuasiHttpCodec.ReadQuasiHttpHeaders(
                readableStream,
                isResponse,
                reqOrStatusLineReceiver,
                headersReceiver,
                connection.ProcessingOptions?.MaxHeadersSize ?? 0,
                connection.CancellationToken);

            if (isResponse)
            {
                var response = new DefaultQuasiHttpResponse();
                response.StatusCode = (int)reqOrStatusLineReceiver[0];
                response.HttpStatusMessage = (string)reqOrStatusLineReceiver[1];
                response.HttpVersion = (string)reqOrStatusLineReceiver[2];
                response.Headers = headersReceiver;
                response.Body = new BodyChunkDecodingStreamInternal(
                    readableStream);
                return response;
            }
            else
            {
                var request = new DefaultQuasiHttpRequest
                {
                    Environment = connection.Environment
                };
                request.HttpMethod = (string)reqOrStatusLineReceiver[0];
                request.Target = (string)reqOrStatusLineReceiver[1];
                request.HttpVersion = (string)reqOrStatusLineReceiver[2];
                request.Headers = headersReceiver;
                request.Body = new BodyChunkDecodingStreamInternal(
                    readableStream);
                return request;
            }
        }

        public static async Task<Stream> BufferResponseBody(
            Stream body, IQuasiHttpConnection connection)
        {
            int bufferingSizeLimit = connection.ProcessingOptions?.ResponseBodyBufferingSizeLimit ?? 0;
            if (bufferingSizeLimit <= 0)
            {
                bufferingSizeLimit = IOUtilsInternal.DefaultDataBufferLimit;
            }
            var buffered = new MemoryStream();
            await IOUtilsInternal.CopyBytesUpToGivenLimit(body,
                buffered, bufferingSizeLimit + 1, connection.CancellationToken);
            if (buffered.Length > bufferingSizeLimit)
            {
                throw new QuasiHttpException(
                    "response body of indeterminate length exceeds buffering limit of " +
                    $"{bufferingSizeLimit} bytes",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            buffered.Position = 0; // reset for reading.
            return buffered;
        }
    }
}
