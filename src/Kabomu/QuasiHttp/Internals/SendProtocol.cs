using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class SendProtocol : ITransferProtocol
    {
        private readonly Dictionary<int, OutgoingTransfer> _outgoingTransfers =
            new Dictionary<int, OutgoingTransfer>();
        private int _requestIdGenerator;

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ProcessOutgoingRequest(QuasiHttpRequestMessage request,
            object connectionHandleOrRemoteEndpoint,
            QuasiHttpSendOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            if (Transport.DirectSendRequestProcessingEnabled)
            {
                Transport.ProcessSendRequest(request, connectionHandleOrRemoteEndpoint, cb);
                return;
            }

            var transfer = new OutgoingTransfer
            {
                TransferProtocol = this,
                RequestId = ++_requestIdGenerator,
                RequestCallback = cb,
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }

            _outgoingTransfers.Add(transfer.RequestId, transfer);
            ResetTimeout(transfer);
            SendRequestPdu(transfer, request, connectionHandleOrRemoteEndpoint);
        }

        private void SendRequestPdu(OutgoingTransfer transfer, QuasiHttpRequestMessage request,
            object connectionHandle)
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
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
                if (request.Body is ByteBufferBody byteBufferBody && pdu.ContentLength <= Transport.MaxPduPayloadSize)
                {
                    pdu.Data = byteBufferBody.Buffer;
                    pdu.DataOffset = byteBufferBody.Offset;
                    pdu.DataLength = byteBufferBody.ContentLength;
                }
                else
                {
                    transfer.RequestBodyProtocol = new OutgoingChunkTransferProtocol(this, transfer,
                        QuasiHttpPdu.PduTypeRequestChunkRet, request.Body);
                }
            }
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
            try
            {
                Transport.SendPdu(pdu, connectionHandle, cb);
            }
            catch (Exception e)
            {
                cancellationIndicator.Cancel();
                HandleSendPduOutcome(transfer, e);
            }
        }

        public void ProcessRequestChunkGetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_outgoingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _outgoingTransfers[pdu.RequestId];
            transfer.RequestBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength, connectionHandle);
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

        private void HandleSendPduOutcome(OutgoingTransfer transfer, Exception e)
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
            if (transfer.ResponseBodyProtocol != null)
            {
                AbortTransfer(transfer, new Exception("incoming response transfer protocol violation"));
                return;
            }

            var response = new QuasiHttpResponseMessage
            {
                StatusIndicatesSuccess = pdu.StatusIndicatesSuccess,
                StatusIndicatesClientError = pdu.StatusIndicatesClientError,
                StatusMessage = pdu.StatusMessage,
                Headers = pdu.Headers
            };
            if (pdu.DataLength > 0)
            {
                response.Body = new ByteBufferBody(pdu.Data, pdu.DataOffset,
                    pdu.DataLength, pdu.ContentType, EventLoop);
            }
            if (pdu.ContentLength != 0 && response.Body == null)
            {
                transfer.ResponseBodyProtocol = new IncomingChunkTransferProtocol(this, transfer,
                    QuasiHttpPdu.PduTypeResponseChunkGet, pdu.ContentLength,
                    pdu.ContentType, connectionHandle);
                response.Body = transfer.ResponseBodyProtocol.Body;
            }

            transfer.RequestCallback.Invoke(null, response);
            transfer.RequestCallback = null;

            if (transfer.ResponseBodyProtocol == null)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
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
            transfer.ResponseBodyProtocol.ProcessChunkRetPdu(pdu.Data,
                pdu.DataOffset, pdu.DataLength, connectionHandle);
        }

        public void ResetTimeout(OutgoingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        public void AbortTransfer(OutgoingTransfer transfer, Exception e)
        {
            if (!_outgoingTransfers.Remove(transfer.RequestId))
            {
                return;
            }
            DisableTransfer(transfer, e);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _outgoingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _outgoingTransfers.Clear();
        }

        private void DisableTransfer(OutgoingTransfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.SendRequestHeaderPduCancellationIndicator?.Cancel();
            transfer.RequestBodyProtocol?.Cancel(e);
            transfer.ResponseBodyProtocol?.Cancel(e);
            transfer.RequestCallback?.Invoke(e, null);

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "outgoing transfer error");
            }
        }
    }
}
