using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
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
        public Func<object, Exception, Task> AbortCallback { get; set; }
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
                Path = chunk.Path,
                Headers = chunk.Headers,
                HttpVersion = chunk.HttpVersion,
                HttpMethod = chunk.HttpMethod
            };
            if (chunk.ContentLength != 0)
            {
                request.Body = new TransportBackedBody(Transport, Connection,
                    chunk.ContentLength, chunk.ContentType, null);
                if (chunk.ContentLength < 0)
                {
                    request.Body = new ChunkDecodingBody(request.Body, MaxChunkSize);
                }
            }

            var response = await Application.ProcessRequest(request, RequestEnvironment); 
            if (response == null)
            {
                throw new Exception("no response");
            }

            try
            {
                await SendResponseLeadChunk(response);
            }
            finally
            {
                // close the response body no matter what.
                if (response.Body != null)
                {
                    await response.Body.EndRead();
                }
            }
        }

        private async Task SendResponseLeadChunk(IQuasiHttpResponse response)
        {
            var chunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                HttpVersion = response.HttpVersion,
                HttpStatusCode = response.HttpStatusCode
            };

            if (response.Body != null)
            {
                chunk.ContentLength = response.Body.ContentLength;
                chunk.ContentType = response.Body.ContentType;
            }

            await ChunkEncodingBody.WriteLeadChunk(Transport, Connection,
                MaxChunkSize, chunk);

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

            await AbortCallback.Invoke(Parent, null);
        }
    }
}
