using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
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
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }

        public Task Cancel()
        {
            return Task.CompletedTask;
        }

        public async Task<IQuasiHttpResponse> Receive()
        {
            // assume properties are set correctly aside the transport and application.
            if (Transport == null)
            {
                throw new MissingDependencyException("server transport");
            }
            if (Application == null)
            {
                throw new MissingDependencyException("server application");
            }

            var transportReaderWriter = new TransportCustomReaderWriter(Transport,
                Connection, false);
            var request = await ReadRequestLeadChunk(transportReaderWriter);

            var response = await Application.ProcessRequest(request);
            if (response == null)
            {
                throw new QuasiHttpRequestProcessingException("no response");
            }

            try
            {
                await TransferResponseToTransport(transportReaderWriter, response);
                return null;
            }
            catch
            {
                try
                {
                    await response.CustomDispose();
                }
                catch (Exception) { } // ignore
                throw;
            }
        }

        private async Task<IQuasiHttpRequest> ReadRequestLeadChunk(ICustomReader reader)
        {
            var chunk = await ChunkedTransferUtils.ReadLeadChunk(reader, MaxChunkSize);

            var request = new DefaultQuasiHttpRequest
            {
                Target = chunk.RequestTarget,
                Headers = chunk.Headers,
                HttpVersion = chunk.HttpVersion,
                Method = chunk.Method,
                Environment = RequestEnvironment
            };
            request.Body = await ProtocolUtilsInternal.CreateBodyFromTransport(Transport,
                Connection, false, MaxChunkSize, chunk.ContentType,
                chunk.ContentLength, false, 0);
            return request;
        }

        private async Task TransferResponseToTransport(ICustomWriter writer,
            IQuasiHttpResponse response)
        {
            var chunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusCode = response.StatusCode,
                Headers = response.Headers,
                HttpVersion = response.HttpVersion,
                HttpStatusMessage = response.HttpStatusMessage,
            };

            if (response.Body != null)
            {
                chunk.ContentLength = response.Body.ContentLength;
                chunk.ContentType = response.Body.ContentType;
            }
            
            await ChunkedTransferUtils.WriteLeadChunk(writer, MaxChunkSize, chunk);

            if (response.Body != null)
            {
                await ProtocolUtilsInternal.TransferBodyToTransport(
                    Transport, Connection, MaxChunkSize, response.Body);
            }

            await response.CustomDispose();
        }
    }
}
