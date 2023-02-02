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

        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public void Cancel()
        {
            if (Connection != null)
            {
                // don't wait.
                _ = Transport.ReleaseConnection(Connection);
            }
            Connection = null;
            Transport = null;
        }

        public async Task<ProtocolSendResult> Send(IQuasiHttpRequest request)
        {
            // assume properties are set correctly aside the transport.
            if (Transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            await SendRequestLeadChunk(request);
            Task<ProtocolSendResult> resFetchTask = StartFetchingResponse();
            if (request.Body != null)
            {
                Task reqTransferTask = TransferRequestBodyToTransport(request.Body);
                // pass resFetchTask first so that hopefully even if both are completed, it
                //  will still win.
                if (await Task.WhenAny(resFetchTask, reqTransferTask) == reqTransferTask)
                {
                    // let any request transfer exceptions terminate entire processing.
                    await reqTransferTask;
                }
            }
            return await resFetchTask;
        }

        private async Task SendRequestLeadChunk(IQuasiHttpRequest request)
        {
            var chunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                RequestTarget = request.Target,
                Headers = request.Headers,
                HttpVersion = request.HttpVersion,
                Method = request.Method
            };

            if (request.Body != null)
            {
                chunk.ContentLength = request.Body.ContentLength;
                chunk.ContentType = request.Body.ContentType;
            }
            await ChunkEncodingBody.WriteLeadChunk(Transport, Connection, chunk, MaxChunkSize);
        }

        private async Task TransferRequestBodyToTransport(IQuasiHttpBody requestBody)
        {
            if (requestBody.ContentLength < 0)
            {
                requestBody = new ChunkEncodingBody(requestBody, MaxChunkSize);
            }
            await TransportUtils.TransferBodyToTransport(Transport,
                Connection, requestBody, MaxChunkSize);
        }

        private async Task<ProtocolSendResult> StartFetchingResponse()
        {
            var chunk = await ChunkDecodingBody.ReadLeadChunk(Transport, Connection,
                MaxChunkSize);
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = chunk.StatusCode,
                Headers = chunk.Headers,
                HttpStatusMessage = chunk.HttpStatusMessage,
                HttpVersion = chunk.HttpVersion,
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
                if (ResponseBufferingEnabled)
                {
                    response.Body = await ProtocolUtilsInternal.CreateEquivalentInMemoryBody(
                        response.Body, MaxChunkSize, ResponseBodyBufferingSizeLimit);
                }
            }

            return new ProtocolSendResult
            {
                Response = response,
                ResponseBufferingApplied = ResponseBufferingEnabled
            };
        }
    }
}
