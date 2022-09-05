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
    internal class DefaultReceiveProtocolInternal
    {
        public object Parent { get; set; }
        public Func<object, Task> AbortCallback { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }

        public Task Receive()
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
            return ReadRequestLeadChunk();
        }

        private async Task ReadRequestLeadChunk()
        {
            var chunk = await ChunkDecodingBody.ReadLeadChunk(Transport, Connection, MaxChunkSize);

            var request = new DefaultQuasiHttpRequest
            {
                Target = chunk.RequestTarget,
                Headers = chunk.Headers,
                HttpVersion = chunk.HttpVersion,
                Method = chunk.Method
            };
            if (chunk.ContentLength != 0)
            {
                request.Body = new TransportBackedBody(Transport, Connection,
                    chunk.ContentLength, false)
                {
                    ContentType = chunk.ContentType
                };
                if (chunk.ContentLength < 0)
                {
                    request.Body = new ChunkDecodingBody(request.Body, MaxChunkSize);
                }
            }

            var response = await Application.ProcessRequest(request, RequestEnvironment); 
            if (response == null)
            {
                throw new ExpectationViolationException("no response");
            }

            // ensure response is closed.
            try
            {
                await TransferResponseToTransport(response);
            }
            finally
            {
                try
                {
                    await response.Close();
                }
                catch (Exception) { }
            }
        }

        private async Task TransferResponseToTransport(IQuasiHttpResponse response)
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

            await ChunkEncodingBody.WriteLeadChunk(Transport, Connection, chunk, MaxChunkSize);

            if (response.Body != null)
            {
                var responseBody = response.Body;
                if (responseBody.ContentLength < 0)
                {
                    responseBody = new ChunkEncodingBody(responseBody, MaxChunkSize);
                }
                await TransportUtils.TransferBodyToTransport(Transport,
                    Connection, responseBody, MaxChunkSize);
            }

            await AbortCallback.Invoke(Parent);
        }
    }
}
