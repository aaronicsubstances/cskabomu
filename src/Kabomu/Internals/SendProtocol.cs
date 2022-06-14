using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class SendProtocol : ITransferProtocol
    {
        private TransportBackedBody _transportBody;
        private IQuasiHttpBody _requestBody, _responseBody;

        public IParentTransferProtocol Parent { get; set; }
        public object Connection { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public Action<Exception, IQuasiHttpResponse> SendCallback { get; set; }

        public void Cancel(Exception e)
        {
            _requestBody?.OnEndRead(Parent.Mutex, e);
            _responseBody?.OnEndRead(Parent.Mutex, e);
        }

        public void OnReceive()
        {
            throw new NotImplementedException("implementation error");
        }

        public void OnSend(IQuasiHttpRequest request)
        {
            SendRequestLeadChunk(request);
        }

        private void SendRequestLeadChunk(IQuasiHttpRequest request)
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
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendRequestLeadChunkOutcome(e, request);
                    }
                }, null);
            };
            ProtocolUtils.WriteLeadChunk(Parent.Transport, Connection, chunk, cb);
        }

        private void HandleSendRequestLeadChunkOutcome(Exception e, IQuasiHttpRequest request)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (request.Body != null)
            {
                if (request.Body.ContentLength < 0)
                {
                    _requestBody = new ChunkEncodingBody(request.Body);
                }
                TransportUtils.TransferBodyToTransport(Parent.Mutex, Parent.Transport, Connection, _requestBody, e => { });
            }
            StartFetchingResponse();
        }

        private void StartFetchingResponse()
        {
            byte[] encodedLength = new byte[2];
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponseLeadChunkLength(e, encodedLength);
                    }
                }, null);
            };
            _transportBody = new TransportBackedBody(Parent.Transport, Connection);
            _transportBody.ContentLength = -1;
            TransportUtils.ReadBytesFully(Parent.Mutex, _transportBody, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleResponseLeadChunkLength(Exception e, byte[] encodedLength)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            int chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                encodedLength.Length);
            var chunkBytes = new byte[chunkLen];
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponseLeadChunk(e, chunkBytes);
                    }
                }, null);
            };
            TransportUtils.ReadBytesFully(Parent.Mutex, _transportBody, chunkBytes, 0, chunkBytes.Length, cb);
        }

        private void HandleResponseLeadChunk(Exception e, byte[] chunkBytes)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
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
                var cancellationIndicator = new STCancellationIndicator();
                ProcessingCancellationIndicator = cancellationIndicator;
                Action closeCb = () =>
                {
                    Parent.Mutex.RunExclusively(_ =>
                    {
                        if (!cancellationIndicator.Cancelled)
                        {
                            cancellationIndicator.Cancel();
                            Parent.AbortTransfer(this, null);
                        }
                    }, null);
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

            SendCallback.Invoke(null, response);
            SendCallback = null;

            if (response.Body == null)
            {
                Parent.AbortTransfer(this, null);
            }
        }
    }
}
