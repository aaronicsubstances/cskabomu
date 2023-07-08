using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
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

        public async Task<ProtocolSendResultInternal> Send()
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

            var transportReaderWriter = new TransportCustomReaderWriter(Transport,
                Connection, false);

            await SendRequestLeadChunk(transportReaderWriter);
            Task<ProtocolSendResultInternal> resFetchTask = StartFetchingResponse(transportReaderWriter);
            if (Request.Body != null)
            {
                Task reqTransferTask = ProtocolUtilsInternal.TransferBodyToTransport(
                    Transport, Connection, MaxChunkSize, Request.Body);
                // pass resFetchTask first so that even if both are completed, it
                //  will still win.
                if (await Task.WhenAny(resFetchTask, reqTransferTask) == reqTransferTask)
                {
                    // let any request transfer exceptions terminate entire processing.
                    await reqTransferTask;
                }
            }
            return await resFetchTask;
        }

        private async Task SendRequestLeadChunk(ICustomWriter writer)
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
            await ChunkedTransferUtils.WriteLeadChunk(writer, MaxChunkSize, chunk);
        }

        private async Task<ProtocolSendResultInternal> StartFetchingResponse(ICustomReader reader)
        {
            var chunk = await ChunkedTransferUtils.ReadLeadChunk(reader,
                MaxChunkSize);
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = chunk.StatusCode,
                Headers = chunk.Headers,
                HttpStatusMessage = chunk.HttpStatusMessage,
                HttpVersion = chunk.HttpVersion,
            };

            response.Body = await ProtocolUtilsInternal.CreateBodyFromTransport(Transport,
                Connection, true, MaxChunkSize, chunk.ContentType,
                chunk.ContentLength, ResponseBufferingEnabled,
                ResponseBodyBufferingSizeLimit);

            return new ProtocolSendResultInternal
            {
                Response = response,
                ResponseBufferingApplied = ResponseBufferingEnabled
            };
        }
    }
}
