using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveProtocolInternal
    {
        private bool _cancelled;
        private IQuasiHttpBody _requestBody, _responseBody;

        public ReceiveProtocolInternal()
        {
        }

        public ReceiveTransferInternal Parent { get; set; }
        public Func<ReceiveTransferInternal, Exception, Task> AbortCallback { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public IMutexApi MutexApi { get; set; }

        public async Task Cancel()
        {
            Task reqBodyEndTask = null, resBodyEndTask = null, releaseTask = null;
            using (await MutexApi.Synchronize())
            {
                /* always cancel so that if no mutex api is being used,
                 * at least the transfer connection will be closed, in order
                 * to forcefully abort the transfer.
                 */
                //if (_cancelled)
                //{
                //    return;
                //}
                reqBodyEndTask = _requestBody?.EndRead();
                resBodyEndTask = _responseBody?.EndRead();
                if (Connection != null)
                {
                    releaseTask = Transport.ReleaseConnection(Connection);
                }

                _cancelled = true;
            }
            if (reqBodyEndTask != null)
            {
                await reqBodyEndTask;
            }
            if (resBodyEndTask != null)
            {
                await resBodyEndTask;
            }
            if (releaseTask != null)
            {
                await releaseTask;
            }
        }

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
            Task<LeadChunk> readTask;
            using (await MutexApi.Synchronize())
            {
                readTask = ChunkDecodingBody.ReadLeadChunk(Transport, Connection, MaxChunkSize);
            }

            var chunk = await readTask;

            Task<IQuasiHttpResponse> appTask;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return;
                }

                var request = new DefaultQuasiHttpRequest
                {
                    Path = chunk.Path,
                    Headers = chunk.Headers,
                    HttpVersion = chunk.HttpVersion,
                    HttpMethod = chunk.HttpMethod
                };
                if (chunk.ContentLength != 0)
                {
                    var transportBody = new TransportBackedBody(Transport, Connection,
                        null, chunk.ContentLength, chunk.ContentType);
                    if (chunk.ContentLength < 0)
                    {
                        request.Body = new ChunkDecodingBody(transportBody, MaxChunkSize);
                    }
                    else
                    {
                        request.Body = transportBody;
                    }
                }
                _requestBody = request.Body;
                appTask = Application.ProcessRequest(request, RequestEnvironment ?? new Dictionary<string, object>());
            }

            var response = await appTask;

            Task sendTask;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return;
                }
                if (response == null)
                {
                    throw new Exception("no response");
                }
                sendTask = SendResponseLeadChunk(response);
            }

            await sendTask;
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

            _responseBody = response.Body;
            if (response.Body != null)
            {
                chunk.ContentLength = response.Body.ContentLength;
                chunk.ContentType = response.Body.ContentType;
            }

            await ChunkEncodingBody.WriteLeadChunk(Transport, Connection,
                MaxChunkSize, chunk);

            Task bodyTransferTask = null;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return;
                }
                if (_responseBody != null)
                {
                    if (_responseBody.ContentLength < 0)
                    {
                        _responseBody = new ChunkEncodingBody(_responseBody, MaxChunkSize);
                    }
                    bodyTransferTask = TransportUtils.TransferBodyToTransport(Transport,
                        Connection, _responseBody, MaxChunkSize);
                }
            }

            if (bodyTransferTask != null)
            {
                await bodyTransferTask;
            }

            Task abortTask;
            using (await MutexApi.Synchronize())
            {
                if (_cancelled)
                {
                    return;
                }
                abortTask = AbortCallback.Invoke(Parent, null);
            }

            await abortTask;
        }
    }
}
