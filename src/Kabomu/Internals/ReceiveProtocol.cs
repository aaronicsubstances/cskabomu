using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ReceiveProtocol : ITransferProtocol
    {
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

        public void OnSend(IQuasiHttpRequest request)
        {
            throw new NotImplementedException("implementation error");
        }

        public void OnReceive()
        {
            ReadRequestLeadChunk();
        }

        private void ReadRequestLeadChunk()
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
                        HandleRequestLeadChunkLength(e, encodedLength);
                    }
                }, null);
            };
            TransportUtils.ReadBytesFully(Parent.Transport, Connection, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleRequestLeadChunkLength(Exception e, byte[] encodedLength)
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
                        HandleRequestLeadChunk(e, chunkBytes);
                    }
                }, null);
            };
            TransportUtils.ReadBytesFully(Parent.Transport, Connection, chunkBytes, 0, chunkBytes.Length, cb);
        }

        private void HandleRequestLeadChunk(Exception e, byte[] chunkBytes)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
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
            if (chunk.HasContent)
            {
                request.Body = new ChunkDecodingBody(
                    chunk.ContentType, Parent.Transport, Connection, null);
            }
            _requestBody = request.Body;

            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, IQuasiHttpResponse> cb = (e, res) =>
            {
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleApplicationProcessingOutcome(e, res);
                    }
                }, null);
            };
            Parent.Application.ProcessRequest(request, cb);
        }

        private void HandleApplicationProcessingOutcome(Exception e, IQuasiHttpResponse response)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (response == null)
            {
                Parent.AbortTransfer(this, new Exception("no response"));
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
                chunk.HasContent = true;
                chunk.ContentType = response.Body.ContentType;
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
                        HandleSendResponseLeadChunkOutcome(e, response);
                    }
                }, null);
            };
            ProtocolUtils.WriteLeadChunk(Parent.Transport, Connection, chunk, cb);
        }

        private void HandleSendResponseLeadChunkOutcome(Exception e, IQuasiHttpResponse response)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (response.Body != null)
            {
                var cancellationIndicator = new STCancellationIndicator();
                ProcessingCancellationIndicator = cancellationIndicator;
                Action<Exception> cb = e2 =>
                {
                    Parent.Mutex.RunExclusively(_ =>
                    {
                        if (!cancellationIndicator.Cancelled)
                        {
                            cancellationIndicator.Cancel();
                            Parent.AbortTransfer(this, e2);
                        }
                    }, null);
                };
                var chunkBody = new ChunkEncodingBody(response.Body);
                TransportUtils.TransferBodyToTransport(Parent.Transport, Connection, chunkBody, Parent.Mutex, cb);
            }
            else
            {
                Parent.AbortTransfer(this, null);
            }
        }
    }
}
