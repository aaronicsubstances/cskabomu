using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ReceiveProtocol
    {
        private static readonly Action<Exception> NullCallback = _ => { };

        private readonly Action<object, bool> RequestChunkReadCallback;

        private readonly Dictionary<int, IncomingTransfer> _incomingTransfers = 
            new Dictionary<int, IncomingTransfer>();

        public ReceiveProtocol()
        {
            RequestChunkReadCallback = OnRequestBodyChunkReadCallback;
        }

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }

        public void ProcessRequestPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            var request = new QuasiHttpRequestMessage
            {
                Path = pdu.Path,
                Headers = pdu.Headers
            };
            var transfer = new IncomingTransfer
            {
                RequestId = pdu.RequestId,
                Request = request,
                TimeoutMillis = DefaultTimeoutMillis,
                ReplyConnectionHandle = connectionHandle
            };
            if (pdu.ContentLength > 0)
            {
                if (pdu.EmbeddedBody != null)
                {
                    request.Body = new ByteBufferBody(pdu.EmbeddedBody, pdu.EmbeddedBodyOffset,
                        pdu.ContentLength, pdu.ContentType, EventLoop);

                    transfer.RequestBodyTransferCompleted = true;
                }
                else
                {
                    request.Body = CreateChunkedRequestBody(transfer, pdu.ContentLength, pdu.ContentType);
                }
            }
            else if (pdu.ContentLength < 0)
            {
                request.Body = CreateChunkedRequestBody(transfer, -1, pdu.ContentType);
            }
            else
            {
                transfer.RequestBodyTransferCompleted = true;
            }
            _incomingTransfers.Add(pdu.RequestId, transfer);
            ResetTimeout(transfer);
            BeginApplicationPipelineProcessing(transfer);
        }

        public void ProcessRequestChunkRetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_incomingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _incomingTransfers[pdu.RequestId];
            if (transfer.RequestBodyTransferCompleted)
            {
                return;
            }
            var chunkedTransferBody = (ChunkedTransferBody)transfer.Request.Body;
            chunkedTransferBody.OnDataWrite(pdu.EmbeddedBody, pdu.EmbeddedBodyOffset,
                pdu.ContentLength);
            ResetTimeout(transfer);
            transfer.ReplyConnectionHandle = connectionHandle;
        }

        public void ProcessResponseChunkGetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_incomingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _incomingTransfers[pdu.RequestId];
            if (transfer.ResponseBodyTransferCompleted)
            {
                return;
            }
            if (IsOperationPending(transfer.ResponseBodyCallbackCancellationIndicator) ||
                IsOperationPending(transfer.SendResponseBodyPduCancellationIndicator))
            {
                AbortTransfer(transfer, new Exception("chunked response transfer protocol violation"));
                return;
            }
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ResponseBodyCallbackCancellationIndicator = cancellationIndicator;
            QuasiHttpBodyCallback cb = (error, data, offset, length) =>
            {
                if (!cancellationIndicator.Cancelled)
                {
                    cancellationIndicator.Cancel();
                    HandleResponseBodyChunk(transfer, error, data, offset, length);
                }
            };
            transfer.Response.Body.OnDataRead(cb);
            transfer.ReplyConnectionHandle = connectionHandle;
        }

        private void HandleResponseBodyChunk(IncomingTransfer transfer, Exception error,
            byte[] data, int offset, int length)
        {
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                AbortTransfer(transfer, new Exception("invalid response body chunk"));
                return;
            }
            SendResponseChunkRetPdu(transfer, data, offset, length);
        }

        private void SendResponseChunkRetPdu(IncomingTransfer transfer, byte[] data, int offset, int length)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeResponseChunkRet,
                RequestId = transfer.RequestId,
                EmbeddedBody = data,
                EmbeddedBodyOffset = offset,
                ContentLength = length
            };
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendResponseBodyPduCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendPduOutcome(transfer, e);
                    }
                }, null);
            };
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, cb);
        }

        public void ProcessResponseFinPdu(QuasiHttpPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _incomingTransfers[pdu.RequestId];
            AbortTransfer(transfer, new Exception("aborted by receiver"));
        }

        private IQuasiHttpBody CreateChunkedRequestBody(IncomingTransfer transfer, int contentLength, string contentType)
        {
            var body = new ChunkedTransferBody(contentLength, contentType, RequestChunkReadCallback, transfer,
                EventLoop);
            return body;
        }

        private void BeginApplicationPipelineProcessing(IncomingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ApplicationProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, QuasiHttpResponseMessage> cb = (ex, res) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleApplicationProcessingOutcome(transfer, ex, res);
                    }
                }, null);
            };
            Application.ProcessRequest(transfer.Request, cb);
        }

        private void HandleApplicationProcessingOutcome(IncomingTransfer transfer, Exception error,
            QuasiHttpResponseMessage response)
        {            
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }

            if (response == null)
            {
                AbortTransfer(transfer, new Exception("null response"));
                return;
            }

            transfer.Response = response;
            SendResponsePdu(transfer, response);

            if (transfer.ResponseBodyTransferCompleted)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
                ResetTimeout(transfer);
            }
        }

        private void SendResponsePdu(IncomingTransfer transfer, QuasiHttpResponseMessage response)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeResponse,
                RequestId = transfer.RequestId,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers
            };

            transfer.ResponseBodyTransferCompleted = true;
            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
                if (response.Body is ByteBufferBody byteBufferBody && pdu.ContentLength <= Transport.MaxPduPayloadSize)
                {
                    pdu.EmbeddedBody = byteBufferBody.Buffer;
                    pdu.EmbeddedBodyOffset = byteBufferBody.Offset;
                }
                else
                {
                    transfer.ResponseBodyTransferCompleted = false;
                }
            }
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendResponseHeaderPduCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendPduOutcome(transfer, e);
                    }
                }, null);
            };
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, cb);
        }

        private void ResetTimeout(IncomingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        private void AbortTransfer(IncomingTransfer transfer, Exception exception)
        {
            if (!_incomingTransfers.Remove(transfer.RequestId))
            {
                return;
            }
            DisableTransfer(transfer, exception);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _incomingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _incomingTransfers.Clear();
        }

        private void DisableTransfer(IncomingTransfer transfer, Exception exception)
        {
            transfer.RequestBodyTransferCompleted = true;
            transfer.ResponseBodyTransferCompleted = true;
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.ApplicationProcessingCancellationIndicator?.Cancel();
            transfer.SendResponseHeaderPduCancellationIndicator?.Cancel();
            transfer.SendRequestBodyPduCancellationIndicator?.Cancel();
            transfer.SendResponseBodyPduCancellationIndicator?.Cancel();
            transfer.ResponseBodyCallbackCancellationIndicator?.Cancel();
        }

        private void OnRequestBodyChunkReadCallback(object obj, bool read)
        {
            var transfer = (IncomingTransfer)obj;
            if (transfer.RequestBodyTransferCompleted)
            {
                return;
            }
            if (read)
            {
                if (IsOperationPending(transfer.SendRequestBodyPduCancellationIndicator))
                {
                    // this means there is a pending chunk get still not resolved.
                    AbortTransfer(transfer, new Exception("chunked request transfer protocol violation"));
                    return;
                }

                SendRequestChunkGetPdu(transfer);
            }
            else
            {
                SendRequestFinPdu(transfer);
            }
        }

        private static bool IsOperationPending(STCancellationIndicator cancellationIndicator)
        {
            return cancellationIndicator != null && !cancellationIndicator.Cancelled;
        }

        private void SendRequestChunkGetPdu(IncomingTransfer transfer)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeRequestChunkGet,
                RequestId = transfer.RequestId
            };
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendRequestBodyPduCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendPduOutcome(transfer, e);
                    }
                }, null);
            };
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, cb);
        }

        private void SendRequestFinPdu(IncomingTransfer transfer)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeRequestFin,
                RequestId = transfer.RequestId
            };
            var pduBytes = pdu.Serialize();
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, NullCallback);
        }

        private void HandleSendPduOutcome(IncomingTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
        }
    }
}
