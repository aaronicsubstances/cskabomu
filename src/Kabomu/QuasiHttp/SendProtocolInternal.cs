using Kabomu.Common;
using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class SendProtocolInternal : ITransferProtocolInternal
    {
        private readonly object _lock;
        private TransportBackedBody _transportBody;
        private IQuasiHttpBody _requestBody, _responseBody;

        public SendProtocolInternal(object lockObj)
        {
            _lock = lockObj;
        }

        public IParentTransferProtocolInternal Parent { get; set; }
        public object Connection { get; set; }
        public bool IsAborted { get; set; }
        public CancellationTokenSource TimeoutCancellationHandle { get; set; }

        public async Task Cancel(Exception e)
        {
            Task reqBodyEndTask = null, resBodyEndTask = null;
            lock (_lock)
            {
                if (_requestBody != null)
                {
                    reqBodyEndTask = _requestBody.EndRead(e);
                }
                if (_responseBody != null)
                {
                    resBodyEndTask = _responseBody.EndRead(e);
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
            lock (_lock)
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
                writeTask = ProtocolUtils.WriteLeadChunk(Parent.Transport, Connection, chunk);
            }

            await writeTask;

            Task <IQuasiHttpResponse> responseFetchTask = null;
            Task transferTask = null;
            lock (_lock)
            {
                if (IsAborted)
                {
                    return null;
                }

                responseFetchTask = StartFetchingResponse();

                if (request.Body.ContentLength < 0)
                {
                    _requestBody = new ChunkEncodingBody(request.Body);
                }
                transferTask = TransportUtils.TransferBodyToTransport(Parent.Transport,
                    Connection, _requestBody);
            }

            // Run a race for pending tasks to obtain any error.
            // if any task finishes first and was successful, then return the result of the response fetch regardless of 
            // any subsequent errors.
            var firstCompletedTask = await Task.WhenAny(transferTask, responseFetchTask);
            await firstCompletedTask;
            return await responseFetchTask;
        }

        private async Task<IQuasiHttpResponse> StartFetchingResponse()
        {
            _transportBody = new TransportBackedBody(Parent.Transport, Connection);
            _transportBody.ContentLength = -1;

            byte[] encodedLength = new byte[2];
            await TransportUtils.ReadBytesFully(_transportBody,
                    encodedLength, 0, encodedLength.Length);

            Task readTask;
            byte[] chunkBytes;
            lock (_lock)
            {
                if (IsAborted)
                {
                    return null;
                }
                int chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                encodedLength.Length);
                chunkBytes = new byte[chunkLen];
                readTask = TransportUtils.ReadBytesFully(_transportBody,
                    chunkBytes, 0, chunkBytes.Length);
            }

            await readTask;

            Task abortTask = null;
            DefaultQuasiHttpResponse response;
            lock (_lock)
            {
                if (IsAborted)
                {
                    return null;
                }

                var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);

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
                    Func<Task> closeCb = async () =>
                    {
                        Task abortTask;
                        lock (_lock)
                        {
                            if (IsAborted)
                            {
                                return;
                            }
                            abortTask = Parent.AbortTransfer(this, null);
                        }
                        await abortTask;
                    };
                    _transportBody.ContentLength = chunk.ContentLength;
                    _transportBody.ContentType = chunk.ContentType;
                    if (chunk.ContentLength < 0)
                    {
                        response.Body = new ChunkDecodingBody(_transportBody, closeCb);
                    }
                    else
                    {
                        _transportBody.CloseCallback = closeCb;
                        response.Body = _transportBody;
                    }
                    _responseBody = response.Body;
                }
                else
                {
                    abortTask = Parent.AbortTransfer(this, null);
                }
            }

            if (abortTask != null)
            {
                await abortTask;
            }

            return response;
        }
    }
}
