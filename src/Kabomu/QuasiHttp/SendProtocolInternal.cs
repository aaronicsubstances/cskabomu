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
    internal class SendProtocolInternal : ITransferProtocolInternal
    {
        private readonly IMutexApi _mutexApi;
        private IQuasiHttpBody _requestBody, _responseBody;

        public SendProtocolInternal(IMutexApi mutexApi)
        {
            _mutexApi = mutexApi ?? new LockBasedMutexApi();
        }

        public IParentTransferProtocolInternal Parent { get; set; }
        public object Connection { get; set; }
        public int MaxChunkSize { get; set; }
        public bool IsAborted { get; set; }
        public CancellationTokenSource TimeoutCancellationHandle { get; set; }

        public async Task Cancel()
        {
            Task reqBodyEndTask = null, resBodyEndTask = null;
            using (await _mutexApi.Synchronize())
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

        public Task<IQuasiHttpResponse> Send(IQuasiHttpRequest request)
        {
            return SendRequestLeadChunk(request);
        }

        private async Task<IQuasiHttpResponse> SendRequestLeadChunk(IQuasiHttpRequest request)
        {
            Task writeTask;
            using (await _mutexApi.Synchronize())
            {
                var chunk = new LeadChunk
                {
                    Version = LeadChunk.Version01,
                    Path = request.Path,
                    Headers = request.Headers,
                    HttpVersion = request.HttpVersion,
                    HttpMethod = request.HttpMethod
                };
                _requestBody = request.Body;
                if (request.Body != null)
                {
                    chunk.ContentLength = request.Body.ContentLength;
                    chunk.ContentType = request.Body.ContentType;
                }
                writeTask = ChunkEncodingBody.WriteLeadChunk(Parent.Transport, Connection,
                    MaxChunkSize, chunk);
            }

            await writeTask;

            Task <IQuasiHttpResponse> responseFetchTask;
            Task bodyTransferTask = null;
            using (await _mutexApi.Synchronize())
            {
                if (IsAborted)
                {
                    return null;
                }

                responseFetchTask = StartFetchingResponse();

                if (_requestBody != null)
                {
                    if (_requestBody.ContentLength < 0)
                    {
                        _requestBody = new ChunkEncodingBody(_requestBody, MaxChunkSize);
                    }
                    bodyTransferTask = TransportUtils.TransferBodyToTransport(Parent.Transport,
                        Connection, _requestBody, MaxChunkSize);
                }
            }

            if (_requestBody == null)
            {
                return await responseFetchTask;
            }

            // Run a race for pending tasks to obtain any error.
            // if any task finishes first and was successful, then return the result of the response fetch regardless of 
            // any subsequent errors.
            var firstCompletedTask = await Task.WhenAny(bodyTransferTask, responseFetchTask);
            await firstCompletedTask;
            return await responseFetchTask;
        }

        private async Task<IQuasiHttpResponse> StartFetchingResponse()
        {
            var chunk = await ChunkDecodingBody.ReadLeadChunk(Parent.Transport, Connection,
                MaxChunkSize);

            Task abortTask = null;
            DefaultQuasiHttpResponse response;
            using (await _mutexApi.Synchronize())
            {
                if (IsAborted)
                {
                    return null;
                }

                response = new DefaultQuasiHttpResponse
                {
                    StatusIndicatesSuccess = chunk.StatusIndicatesSuccess,
                    StatusIndicatesClientError = chunk.StatusIndicatesClientError,
                    StatusMessage = chunk.StatusMessage,
                    Headers = chunk.Headers,
                    HttpVersion = chunk.HttpVersion,
                    HttpStatusCode = chunk.HttpStatusCode
                };

                if (chunk.ContentLength != 0)
                {
                    var transportBody = new TransportBackedBody(Parent.Transport, Connection,
                        CloseTransfer, chunk.ContentLength, chunk.ContentType);
                    if (chunk.ContentLength < 0)
                    {
                        response.Body = new ChunkDecodingBody(transportBody, MaxChunkSize);
                    }
                    else
                    {
                        response.Body = transportBody;
                    }
                    _responseBody = response.Body;
                }
                else
                {
                    abortTask = Parent.AbortTransfer(this);
                }
            }

            if (abortTask != null)
            {
                await abortTask;
            }

            return response;
        }

        private async Task CloseTransfer()
        {
            Task abortTask;
            using (await _mutexApi.Synchronize())
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
