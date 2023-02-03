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

        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseBufferingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }

        public async Task Cancel()
        {
            // just in case Transport was incorrectly set to null.
            if (Transport != null)
            {
                await Transport.ReleaseConnection(Connection);
            }
        }

        public async Task<ProtocolSendResult> Send()
        {
            // assume properties are set correctly aside the transport.
            if (Transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            if (Request == null)
            {
                throw new ExpectationViolationException("request");
            }

            await SendRequestLeadChunk();
            Task<ProtocolSendResult> resFetchTask = StartFetchingResponse();
            if (Request.Body != null)
            {
                Task reqTransferTask = TransferRequestBodyToTransport(Request.Body);
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

        private async Task SendRequestLeadChunk()
        {
            var chunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                RequestTarget = Request.Target,
                Headers = Request.Headers,
                HttpVersion = Request.HttpVersion,
                Method = Request.Method
            };

            if (Request.Body != null)
            {
                chunk.ContentLength = Request.Body.ContentLength;
                chunk.ContentType = Request.Body.ContentType;
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
