using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class SendProtocol
    {
        private static readonly Action<Exception> NullCallback = _ => { };

        private readonly Action<object, bool> ResponseChunkReadCallback;

        private readonly Dictionary<int, OutgoingTransfer> _outgoingTransfers =
            new Dictionary<int, OutgoingTransfer>();
        private int _requestIdGenerator;

        public SendProtocol()
        {
            ResponseChunkReadCallback = OnResponseBodyChunkReadCallback;
        }

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ProcessOutgoingRequest(QuasiHttpRequestMessage request, QuasiHttpPostOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            var transfer = new OutgoingTransfer
            {
                RequestId = ++_requestIdGenerator,
                Request = request,
                RequestCallback = cb
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
                transfer.ReplyConnectionHandle = options.ConnectionHandle;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }

            _outgoingTransfers.Add(transfer.RequestId, transfer);
            ResetTimeout(transfer);
            if (Transport.DirectSendRequestProcessingEnabled)
            {
                SendRequestDirectly(transfer, request);
            }
            else
            {
                SendRequestPdu(transfer, request);
            }
        }

        public void ProcessRequestChunkGetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_outgoingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _outgoingTransfers[pdu.RequestId];
            if (transfer.RequestBodyTransferCompleted)
            {
                return;
            }
            if (IsOperationPending(transfer.RequestBodyCallbackCancellationIndicator) ||
                IsOperationPending(transfer.SendRequestBodyPduCancellationIndicator))
            {
                AbortTransfer(transfer, new Exception("chunked request transfer protocol violation"));
                return;
            }
            var cancellationIndicator = new STCancellationIndicator();
            transfer.RequestBodyCallbackCancellationIndicator = cancellationIndicator;
            QuasiHttpBodyCallback cb = (error, data, offset, length) =>
            {
                if (!cancellationIndicator.Cancelled)
                {
                    cancellationIndicator.Cancel();
                    HandleRequestBodyChunk(transfer, error, data, offset,length);
                }
            };
            transfer.Request.Body.OnDataRead(cb);
            transfer.ReplyConnectionHandle = connectionHandle;
        }

        public void ProcessRequestFinPdu(QuasiHttpPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _outgoingTransfers[pdu.RequestId];
            ResetTimeout(transfer);
        }

        public void ProcessResponsePdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_outgoingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _outgoingTransfers[pdu.RequestId];
            if (transfer.Response != null)
            {
                AbortTransfer(transfer, new Exception("duplicate response pdu detected"));
                return;
            }
            transfer.ReplyConnectionHandle = connectionHandle;

            var response = new QuasiHttpResponseMessage
            {
                StatusIndicatesSuccess = pdu.StatusIndicatesSuccess,
                StatusIndicatesClientError = pdu.StatusIndicatesClientError,
                StatusMessage = pdu.StatusMessage,
                Headers = pdu.Headers
            };
            if (pdu.ContentLength > 0)
            {
                if (pdu.EmbeddedBody != null)
                {
                    response.Body = new ByteBufferBody(pdu.EmbeddedBody, pdu.EmbeddedBodyOffset,
                        pdu.ContentLength, pdu.ContentType, EventLoop);
                    transfer.ResponseBodyTransferCompleted = true;
                }
                else
                {
                    response.Body = CreateChunkedResponseBody(transfer, pdu.ContentLength, pdu.ContentType);
                }
            }
            else if (pdu.ContentLength < 0)
            {
                response.Body = CreateChunkedResponseBody(transfer, -1, pdu.ContentType);
            }
            else
            {
                transfer.ResponseBodyTransferCompleted = true;
            }

            transfer.Response = response;

            if (transfer.ResponseBodyTransferCompleted)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
                transfer.RequestCallback.Invoke(null, response);
                transfer.RequestCallback = null;
                ResetTimeout(transfer);
            }
        }

        public void ProcessResponseChunkRetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_outgoingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _outgoingTransfers[pdu.RequestId];
            if (transfer.ResponseBodyTransferCompleted)
            {
                return;
            }
            var chunkedTransferBody = (ChunkedTransferBody)transfer.Response.Body;
            chunkedTransferBody.OnDataWrite(pdu.EmbeddedBody, pdu.EmbeddedBodyOffset,
                pdu.ContentLength);
            ResetTimeout(transfer);
            transfer.ReplyConnectionHandle = connectionHandle;
        }

        private void HandleRequestBodyChunk(OutgoingTransfer transfer, Exception error,
            byte[] data, int offset, int length)
        {
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                AbortTransfer(transfer, new Exception("invalid request body chunk"));
                return;
            }
            SendRequestChunkRetPdu(transfer, data, offset, length);
        }

        private void SendRequestChunkRetPdu(OutgoingTransfer transfer, byte[] data, int offset, int length)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeRequestChunkRet,
                RequestId = transfer.RequestId,
                EmbeddedBody = data,
                EmbeddedBodyOffset = offset,
                ContentLength = length
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

        private void SendRequestDirectly(OutgoingTransfer transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.DirectRequestProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, QuasiHttpResponseMessage> cb = (ex, res) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleDirectRequestProcessingOutcome(transfer, ex, res);
                    }
                }, null);
            };
            Transport.ProcessSendRequest(request, cb);
        }

        private void HandleDirectRequestProcessingOutcome(OutgoingTransfer transfer, Exception ex,
            QuasiHttpResponseMessage res)
        {
            if (ex == null)
            {
                AbortTransfer(transfer, ex);
                return;
            }
            if (res == null)
            {
                AbortTransfer(transfer, new Exception("null response"));
                return;
            }

            transfer.Response = res;
            AbortTransfer(transfer, null);
        }

        private void SendRequestPdu(OutgoingTransfer transfer, QuasiHttpRequestMessage request)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeRequest,
                RequestId = transfer.RequestId,
                Path = request.Path,
                Headers = request.Headers
            };

            transfer.RequestBodyTransferCompleted = true;
            if (request.Body != null)
            {
                pdu.ContentLength = request.Body.ContentLength;
                pdu.ContentType = request.Body.ContentType;
                if (request.Body is ByteBufferBody byteBufferBody && pdu.ContentLength <= Transport.MaxPduPayloadSize)
                {
                    pdu.EmbeddedBody = byteBufferBody.Buffer;
                    pdu.EmbeddedBodyOffset = byteBufferBody.Offset;
                }
                else
                {
                    transfer.RequestBodyTransferCompleted = false;
                }
            }
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendRequestHeaderPduCancellationIndicator = cancellationIndicator;
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

        private IQuasiHttpBody CreateChunkedResponseBody(OutgoingTransfer transfer, int contentLength, string contentType)
        {
            var body = new ChunkedTransferBody(contentLength, contentType, ResponseChunkReadCallback, transfer,
                EventLoop);
            return body;
        }

        private void ResetTimeout(OutgoingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        private void AbortTransfer(OutgoingTransfer transfer, Exception exception)
        {
            if (!_outgoingTransfers.Remove(transfer.RequestId))
            {
                return;
            }
            DisableTransfer(transfer, exception);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _outgoingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _outgoingTransfers.Clear();
        }

        private void DisableTransfer(OutgoingTransfer transfer, Exception exception)
        {
            transfer.RequestBodyTransferCompleted = true;
            transfer.ResponseBodyTransferCompleted = true;
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.DirectRequestProcessingCancellationIndicator?.Cancel();
            transfer.SendRequestHeaderPduCancellationIndicator?.Cancel();
            transfer.SendRequestBodyPduCancellationIndicator?.Cancel();
            transfer.SendResponseBodyPduCancellationIndicator?.Cancel();
            transfer.RequestBodyCallbackCancellationIndicator?.Cancel();

            transfer.RequestCallback?.Invoke(exception, transfer.Response);
        }

        private void OnResponseBodyChunkReadCallback(object obj, bool read)
        {
            var transfer = (OutgoingTransfer)obj;
            if (transfer.ResponseBodyTransferCompleted)
            {
                return;
            }
            if (read)
            {
                if (IsOperationPending(transfer.SendResponseBodyPduCancellationIndicator))
                {
                    // this means there is a pending chunk get still not resolved.
                    AbortTransfer(transfer, new Exception("chunked response transfer protocol violation"));
                    return;
                }

                SendResponseChunkGetPdu(transfer);
            }
            else
            {
                AbortTransfer(transfer, new Exception("response body closed"));
                SendResponseFinPdu(transfer);
            }
        }

        private static bool IsOperationPending(STCancellationIndicator cancellationIndicator)
        {
            return cancellationIndicator != null && !cancellationIndicator.Cancelled;
        }

        private void SendResponseChunkGetPdu(OutgoingTransfer transfer)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeResponseChunkGet,
                RequestId = transfer.RequestId
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

        private void SendResponseFinPdu(OutgoingTransfer transfer)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeResponseFin,
                RequestId = transfer.RequestId
            };
            var pduBytes = pdu.Serialize();
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, NullCallback);
        }

        private void HandleSendPduOutcome(OutgoingTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
        }
    }
}
