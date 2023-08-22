using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class DefaultReceiveProtocolInternal : IReceiveProtocolInternal
    {
        public DefaultReceiveProtocolInternal()
        {
            
        }

        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }

        public async Task Cancel()
        {
            // just in case Transport was incorrectly set to null.
            if (Transport != null)
            {
                await Transport.ReleaseConnection(Connection);
            }
        }

        public async Task<IQuasiHttpResponse> Receive()
        {
            if (Transport == null)
            {
                throw new MissingDependencyException("server transport");
            }
            if (Application == null)
            {
                throw new MissingDependencyException("server application");
            }

            var request = await ReadRequestLeadChunk();

            var response = await Application.ProcessRequest(request);
            if (response == null)
            {
                throw new QuasiHttpRequestProcessingException("no response");
            }

            try
            {
                await TransferResponseToTransport(response);
                return null;
            }
            finally
            {
                try
                {
                    await response.Release();
                }
                catch (Exception) { } // ignore
            }
        }

        private async Task<IQuasiHttpRequest> ReadRequestLeadChunk()
        {
            var reader = Transport.GetReader(Connection);
            var chunk = await new ChunkedTransferCodec().ReadLeadChunk(reader, MaxChunkSize);
            if (chunk == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            var request = new DefaultQuasiHttpRequest
            {
                Environment = RequestEnvironment
            };
            ChunkedTransferCodec.UpdateRequest(request, chunk);
            request.Body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, chunk.ContentLength, null,
                MaxChunkSize, false, 0);
            return request;
        }

        private async Task TransferResponseToTransport(IQuasiHttpResponse response)
        {
            if (ProtocolUtilsInternal.GetEnvVarAsBoolean(response.Environment,
                QuasiHttpUtils.ResEnvKeySkipResponseSending) == true)
            {
                return;
            }

            var leadChunk = ChunkedTransferCodec.CreateFromResponse(response);
            var writer = Transport.GetWriter(Connection);
            await new ChunkedTransferCodec().WriteLeadChunk(writer, leadChunk, MaxChunkSize);
            await ProtocolUtilsInternal.TransferBodyToTransport(
                writer, MaxChunkSize, response.Body, leadChunk.ContentLength);
        }
    }
}
