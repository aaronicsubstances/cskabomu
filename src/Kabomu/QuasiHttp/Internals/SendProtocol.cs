using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class SendProtocol
    {
        private readonly Dictionary<int, OutgoingTransfer> _outgoingTransfers =
            new Dictionary<int, OutgoingTransfer>();
        private int _requestIdGenerator;

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
            if (request.Body != null)
            {
                pdu.ContentLength = request.Body.ContentLength;
                pdu.ContentType = request.Body.ContentType;
                if (request.Body is ByteBufferBody byteBufferBody)
                {
                    // TODO: also check that size can fit into max pdu payload of 30KB.
                    pdu.EmbeddedBody = byteBufferBody.Buffer;
                    pdu.EmbeddedBodyOffset = byteBufferBody.Offset;
                }
                else
                {
                    transfer.RequestBodyTransferRequired = true;
                }
            }
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendPduCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendRequestPduOutcome(transfer, e);
                    }
                }, null);
            };
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, cb);
        }

        private void HandleSendRequestPduOutcome(OutgoingTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
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
                }
                else
                {
                    response.Body = CreateNonChunkedResponseBody(pdu.RequestId, pdu.ContentLength, pdu.ContentType);
                    transfer.ResponseBodyTransferRequired = true;
                }
            }
            else if (pdu.ContentLength < 0)
            {
                response.Body = CreateChunkedResponseBody(pdu.RequestId, pdu.ContentType);
                transfer.ResponseBodyTransferRequired = true;
            }

            transfer.Response = response;

            if (transfer.ResponseBodyTransferRequired)
            {
                transfer.RequestCallback.Invoke(null, response);
                transfer.RequestCallback = null;
                ResetTimeout(transfer);
            }
            else
            {
                AbortTransfer(transfer, null);
            }
        }

        private IQuasiHttpBody CreateChunkedResponseBody(int requestId, string contentType)
        {
            throw new NotImplementedException();
        }

        private IQuasiHttpBody CreateNonChunkedResponseBody(int requestId, int contentLength, string contentType)
        {
            throw new NotImplementedException();
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
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = null;
            transfer.SendPduCancellationIndicator?.Cancel();
            transfer.SendPduCancellationIndicator = null;
            transfer.DirectRequestProcessingCancellationIndicator?.Cancel();
            transfer.DirectRequestProcessingCancellationIndicator = null;
            transfer.RequestCallback?.Invoke(exception, transfer.Response);
            transfer.RequestCallback = null;
            transfer.Request = null;
            transfer.Response = null;
        }
    }
}
