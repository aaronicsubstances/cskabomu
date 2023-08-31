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
            if (reader == null)
            {
                throw new QuasiHttpRequestProcessingException("no reader for connection");
            }

            var chunk = await new CustomChunkedTransferCodec().ReadLeadChunk(reader, MaxChunkSize);
            if (chunk == null)
            {
                throw new QuasiHttpRequestProcessingException("no request");
            }
            var request = new DefaultQuasiHttpRequest
            {
                Environment = RequestEnvironment
            };
            CustomChunkedTransferCodec.UpdateRequest(request, chunk);
            request.Body = await ProtocolUtilsInternal.CreateBodyFromTransport(
                reader, chunk.ContentLength, null,
                false, 0);
            return request;
        }

        private async Task TransferResponseToTransport(IQuasiHttpResponse response)
        {
            if (ProtocolUtilsInternal.GetEnvVarAsBoolean(response.Environment,
                QuasiHttpUtils.ResEnvKeySkipResponseSending) == true)
            {
                return;
            }

            var writer = Transport.GetWriter(Connection);
            if (writer == null)
            {
                throw new QuasiHttpRequestProcessingException("no writer for connection");
            }
            var leadChunk = CustomChunkedTransferCodec.CreateFromResponse(response);
            await new CustomChunkedTransferCodec().WriteLeadChunk(writer, leadChunk, MaxChunkSize);
            await ProtocolUtilsInternal.TransferBodyToTransport(
                writer, response.Body, leadChunk.ContentLength);
        }
    }
}
