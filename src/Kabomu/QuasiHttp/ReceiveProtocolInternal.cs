using Kabomu.Common;
using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveProtocolInternal : ITransferProtocolInternal
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

        public Task<IQuasiHttpResponse> SendAsync(IQuasiHttpRequest request)
        {
            throw new NotImplementedException("implementation error");
        }

        public async Task ReceiveAsync()
        {
            if (Parent.EventLoop.IsMutexRequired(out Task mt)) await mt;

            await ReadRequestLeadChunkAsync();
        }

        private async Task ReadRequestLeadChunkAsync()
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
                return;
            }

            if (readError != null)
            {
                await Parent.AbortTransferAsync(this, readError);
                return;
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
                return;
            }

            if (readError != null)
            {
                await Parent.AbortTransferAsync(this, readError);
                return;
            }

            var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
            var request = new DefaultQuasiHttpRequest
            {
                Path = chunk.Path,
                Headers = chunk.Headers,
                HttpVersion = chunk.HttpVersion,
                HttpMethod = chunk.HttpMethod
            };
            if (chunk.ContentLength != 0)
            {
                _transportBody.ContentLength = chunk.ContentLength;
                _transportBody.ContentType = chunk.ContentType;
                if (chunk.ContentLength < 0)
                {
                    request.Body = new ChunkDecodingBody(_transportBody, null);
                }
                else
                {
                    request.Body = _transportBody;
                }
            }
            _requestBody = request.Body;

            
            // Begin Application processing.
            try
            {
                var res = await Parent.EventLoop.MutexWrap(Parent.Application.ProcessRequestAsync(request));
                if (!IsAborted)
                {
                    await SendResponseLeadChunkAsync(res);
                }
                return;
            }
            catch (Exception e)
            {
                if (!IsAborted)
                {
                    await Parent.AbortTransferAsync(this, e);
                }
            }
        }

        private async Task SendResponseLeadChunkAsync(IQuasiHttpResponse response)
        {
            if (response == null)
            {
                await Parent.AbortTransferAsync(this, new Exception("no response"));
                return;
            }

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
                return;
            }

            if (writeError != null)
            {
                await Parent.AbortTransferAsync(this, writeError);
                return;
            }

            if (response.Body != null)
            {
                await Parent.EventLoop.MutexWrap(TransportUtils.TransferBodyToTransportAsync(Parent.EventLoop, Parent.Transport,
                    Connection, _responseBody));
                if (!IsAborted)
                {
                    await Parent.AbortTransferAsync(this, null);
                }
            }
            else
            {
                await Parent.AbortTransferAsync(this, null);
            }
        }
    }
}
