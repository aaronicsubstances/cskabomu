using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveProtocolInternal : ITransferProtocolInternal
    {
        private IQuasiHttpBody _requestBody, _responseBody;

        public ReceiveProtocolInternal()
        {
        }

        public IParentTransferProtocolInternal Parent { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public bool IsAborted { get; set; }
        public CancellationTokenSource TimeoutCancellationHandle { get; set; }
        public IMutexApi MutexApi { get; set; }

        public async Task Cancel()
        {
            Task reqBodyEndTask = null, resBodyEndTask = null;
            using (await MutexApi.Synchronize())
            {
                if (_requestBody != null)
                {
                    reqBodyEndTask = _requestBody.EndRead();
                }
                if (_responseBody != null)
                {
                    resBodyEndTask = _responseBody.EndRead();
                }
            }
            if (reqBodyEndTask != null)
            {
                await reqBodyEndTask;
            }
            if (resBodyEndTask != null)
            {
                await resBodyEndTask;
            }
        }

        public Task Receive()
        {
            return ReadRequestLeadChunk();
        }

        private async Task ReadRequestLeadChunk()
        {
            Task<LeadChunk> readTask;
            using (await MutexApi.Synchronize())
            {
                readTask = ChunkDecodingBody.ReadLeadChunk(Parent.Transport, Connection, MaxChunkSize);
            }

            var chunk = await readTask;

            Task<IQuasiHttpResponse> appTask;
            using (await MutexApi.Synchronize())
            {
                if (IsAborted)
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
                    var transportBody = new TransportBackedBody(Parent.Transport, Connection,
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
                appTask = Parent.Application.ProcessRequest(request,
                    RequestEnvironment ?? new Dictionary<string, object>());
            }

            var response = await appTask;

            Task sendTask;
            using (await MutexApi.Synchronize())
            {
                if (IsAborted)
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

            await ChunkEncodingBody.WriteLeadChunk(Parent.Transport, Connection,
                MaxChunkSize, chunk);

            Task bodyTransferTask = null;
            using (await MutexApi.Synchronize())
            {
                if (IsAborted)
                {
                    return;
                }
                if (_responseBody != null)
                {
                    if (_responseBody.ContentLength < 0)
                    {
                        _responseBody = new ChunkEncodingBody(_responseBody, MaxChunkSize);
                    }
                    bodyTransferTask = TransportUtils.TransferBodyToTransport(Parent.Transport,
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
                if (IsAborted)
                {
                    return;
                }
                abortTask = Parent.AbortTransfer(this);
            }

            await abortTask;
        }
    }
}
