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
        private TransportBackedBody _transportBody;
        private IQuasiHttpBody _requestBody, _responseBody;

        public IParentTransferProtocolInternal Parent { get; set; }
        public object Connection { get; set; }
        public bool IsAborted { get; set; }
        public int TimeoutMillis { get; set; }
        public CancellationTokenSource TimeoutCancellationHandle { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> SendCallback { get; set; }

        public async Task CancelAsync(Exception e)
        {
            if (Parent.EventLoop.IsMutexRequired(out Task mt)) await mt;

            if (_requestBody != null)
            {
                await _requestBody.EndReadAsync(Parent.EventLoop, e);
            }
            if (_responseBody != null)
            {
                await _responseBody.EndReadAsync(Parent.EventLoop, e);
            }
        }

        public Task ReceiveAsync()
        {
            throw new NotImplementedException("implementation error");
        }

        public async Task<IQuasiHttpResponse> SendAsync(IQuasiHttpRequest request)
        {
            if (Parent.EventLoop.IsMutexRequired(out Task mt)) await mt;

            return await SendRequestLeadChunkAsync(request);
        }

        private async Task<IQuasiHttpResponse> SendRequestLeadChunkAsync(IQuasiHttpRequest request)
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
            Exception writeError = null;
            try
            {
                await Parent.EventLoop.MutexWrap(ProtocolUtils.WriteLeadChunkAsync(Parent.Transport, Connection, chunk));
            }
            catch (Exception e)
            {
                writeError = e;
            }

            if (IsAborted)
            {
                return null;
            }

            if (writeError != null)
            {
                return await Parent.AbortTransferAsync(this, writeError);
            }

            var responseFetchTask = StartFetchingResponseAsync();
            if (request.Body == null)
            {
                return await responseFetchTask;
            }

            if (request.Body.ContentLength < 0)
            {
                _requestBody = new ChunkEncodingBody(request.Body);
            }
            var transferTask = TransportUtils.TransferBodyToTransportAsync(Parent.EventLoop, Parent.Transport, 
                Connection, _requestBody);

            // Run a race for pending tasks to obtain the first success or error.
            // if any task finishes first and was successful, then return the result of the send callback regardless of 
            // any subsequent errors.
            var firstCompletedTask = await Task.WhenAny(SendCallback.Task, transferTask, responseFetchTask);
            await firstCompletedTask;
            return await SendCallback.Task;
        }

        private async Task<IQuasiHttpResponse> StartFetchingResponseAsync()
        {
            _transportBody = new TransportBackedBody(Parent.Transport, Connection);
            _transportBody.ContentLength = -1;

            byte[] encodedLength = new byte[2];
            Exception readError = null;
            try
            {
                await Parent.EventLoop.MutexWrap(TransportUtils.ReadBytesFullyAsync(Parent.EventLoop, _transportBody, 
                    encodedLength, 0, encodedLength.Length));
            }
            catch (Exception e)
            {
                readError = e;
            }

            if (IsAborted)
            {
                return null;
            }

            if (readError != null)
            {
                return await Parent.AbortTransferAsync(this, readError);
            }

            int chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                encodedLength.Length);
            var chunkBytes = new byte[chunkLen];

            readError = null;
            try
            {
                await Parent.EventLoop.MutexWrap(TransportUtils.ReadBytesFullyAsync(Parent.EventLoop, _transportBody,
                    chunkBytes, 0, chunkBytes.Length));
            }
            catch (Exception e)
            {
                readError = e;
            }

            if (IsAborted)
            {
                return null;
            }

            if (readError != null)
            {
                return await Parent.AbortTransferAsync(this, readError);
            }

            var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);

            var response = new DefaultQuasiHttpResponse
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
                    if (Parent.EventLoop.IsMutexRequired(out Task mt)) await mt;

                    if (!IsAborted)
                    {
                        await Parent.AbortTransferAsync(this, null);
                    }
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
            }
            _responseBody = response.Body;

            SendCallback.SetResult(response);

            if (response.Body == null)
            {
                return await Parent.AbortTransferAsync(this, null);
            }
            else
            {
                return await SendCallback.Task;
            }
        }
    }
}
