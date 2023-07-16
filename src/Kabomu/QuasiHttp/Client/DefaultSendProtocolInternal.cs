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

        public Func<IDictionary<string, object>, Task<IQuasiHttpRequest>> RequestFunc { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
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
            if (Transport == null)
            {
                throw new MissingDependencyException("client transport");
            }
            if (RequestFunc == null)
            {
                throw new ExpectationViolationException("request func");
            }

            var request = await RequestFunc.Invoke(RequestEnvironment);
            if (request == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }

            var transportReaderWriter = new TransportCustomReaderWriter(Transport,
                Connection, false);

            await SendRequestLeadChunk(request, transportReaderWriter);
            // NB: tests depend on request body transfer started before
            // reading of response.
            var reqTransferTask = ProtocolUtilsInternal.TransferBodyToTransport(
                Transport, Connection, MaxChunkSize, request.Body);
            var resFetchTask = StartFetchingResponse(transportReaderWriter);
            // pass resFetchTask first so that even if both are completed, it
            //  will still win.
            if (await Task.WhenAny(resFetchTask, reqTransferTask) == reqTransferTask)
            {
                // let any request transfer exceptions terminate entire processing.
                await reqTransferTask;
            }
            return await resFetchTask;
        }

        private async Task SendRequestLeadChunk(IQuasiHttpRequest request,
            ICustomWriter writer)
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
