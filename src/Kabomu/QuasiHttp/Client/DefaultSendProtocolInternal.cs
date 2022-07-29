using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class DefaultSendProtocolInternal : ISendProtocolInternal
    {
        public DefaultSendProtocolInternal()
        {
        }

        public object Parent { get; set; }
        public Func<object, Exception, IQuasiHttpResponse, Task> AbortCallback { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseStreamingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public Task Cancel()
        {
            return Transport.ReleaseConnection(Connection);
        }

        public Task<IQuasiHttpResponse> Send(IQuasiHttpRequest request)
        {
            // assume properties are set correctly aside the transport.
            if (Transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            return SendRequestLeadChunk(request);
        }

        private async Task<IQuasiHttpResponse> SendRequestLeadChunk(IQuasiHttpRequest request)
        {
            var chunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                Path = request.Path,
                Headers = request.Headers,
                HttpVersion = request.HttpVersion,
                HttpMethod = request.HttpMethod
            };
            if (request.Body != null)
            {
                chunk.ContentLength = request.Body.ContentLength;
                chunk.ContentType = request.Body.ContentType;
            }
            await ChunkEncodingBody.WriteLeadChunk(Transport, Connection, chunk, MaxChunkSize);

            if (request.Body != null)
            {
                var requestBody = request.Body;
                if (requestBody.ContentLength < 0)
                {
                    requestBody = new ChunkEncodingBody(requestBody, MaxChunkSize);
                }
                _ = TransferRequestBodyToTransport(requestBody);
            }

            var res = await StartFetchingResponse();
            return res;
        }

        private async Task TransferRequestBodyToTransport(IQuasiHttpBody requestBody)
        {
            try
            {
                await TransportUtils.TransferBodyToTransport(Transport,
                    Connection, requestBody, MaxChunkSize);
            }
            catch (Exception e)
            {
                await AbortCallback.Invoke(Parent, e, null);
            }
        }

        private async Task<IQuasiHttpResponse> StartFetchingResponse()
        {
            var chunk = await ChunkDecodingBody.ReadLeadChunk(Transport, Connection,
                MaxChunkSize);
            var response = new DefaultQuasiHttpResponse
            {
                StatusIndicatesSuccess = chunk.StatusIndicatesSuccess,
                StatusIndicatesClientError = chunk.StatusIndicatesClientError,
                StatusMessage = chunk.StatusMessage,
                Headers = chunk.Headers,
                HttpVersion = chunk.HttpVersion,
                HttpStatusCode = chunk.HttpStatusCode
            };

            if (chunk.ContentLength != 0)
            {
                response.Body = new TransportBackedBody(Transport, Connection,
                    chunk.ContentLength, true)
                {
                    ContentType = chunk.ContentType
                };
                if (chunk.ContentLength < 0)
                {
                    response.Body = new ChunkDecodingBody(response.Body, MaxChunkSize);
                }
                if (!ResponseStreamingEnabled)
                {
                    response.Body = await ProtocolUtilsInternal.CreateEquivalentInMemoryBody(
                        response.Body, MaxChunkSize, ResponseBodyBufferingSizeLimit);
                }
            }

            await AbortCallback.Invoke(Parent, null, response);
            return response;
        }
    }
}
