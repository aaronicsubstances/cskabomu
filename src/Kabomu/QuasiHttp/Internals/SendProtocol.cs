using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class SendProtocol : ITransferProtocol
    {
        private readonly Dictionary<object, Transfer> _outgoingTransfers =
            new Dictionary<object, Transfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ProcessOutgoingRequest(object remoteEndpoint, 
            QuasiHttpRequestMessage request,            
            QuasiHttpSendOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            var transfer = new Transfer
            {
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
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
               _ =>
               {
                   DisableTransfer(transfer, new Exception("send timeout"));
               }, null);
            if (Transport.DirectSendRequestProcessingEnabled)
            {
                ProcessSendRequestDirectly(remoteEndpoint, transfer, request);
            }
            else
            {
                AllocateConnection(remoteEndpoint, transfer, request);
            }
        }

        private void ProcessSendRequestDirectly(object remoteEndpoint, Transfer transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendRequestCancellationIndicator = cancellationIndicator;
            Action<Exception, QuasiHttpResponseMessage> cb = (e, res) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (cancellationIndicator.Cancelled)
                    {
                        return;
                    }
                    cancellationIndicator.Cancel();
                    HandleDirectSendRequestProcessingOutcome(e, res, transfer);
                }, null);
            };
            try
            {
                Transport.ProcessSendRequest(remoteEndpoint, request, cb);
            }
            catch (Exception e)
            {
                DisableTransfer(transfer, e);
            }
        }

        private void HandleDirectSendRequestProcessingOutcome(Exception e, QuasiHttpResponseMessage res,
            Transfer transfer)
        {
            if (e != null)
            {
                DisableTransfer(transfer, e);
                return;
            }

            if (res == null)
            {
                DisableTransfer(transfer, new Exception("no response"));
                return;
            }

            transfer.RequestCallback.Invoke(e, res);
            transfer.RequestCallback = null;
            DisableTransfer(transfer, null);
        }

        private void AllocateConnection(object remoteEndpoint, Transfer transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendRequestCancellationIndicator = cancellationIndicator;
            Action<Exception, object> cb = (e, connection) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (cancellationIndicator.Cancelled)
                    {
                        return;
                    }
                    cancellationIndicator.Cancel();
                    HandleConnectionAllocationOutcome(e, connection, transfer, request);
                }, null);
            };
            try
            {
                Transport.AllocateConnection(remoteEndpoint, cb);
            }
            catch (Exception e)
            {
                DisableTransfer(transfer, e);
            }
        }

        private void HandleConnectionAllocationOutcome(Exception e, object connection, Transfer transfer, 
            QuasiHttpRequestMessage request)
        {
            if (e != null)
            {
                DisableTransfer(transfer, e);
                return;
            }

            if (connection == null)
            {
                DisableTransfer(transfer, new Exception("no connection created"));
                return;
            }

            transfer.Connection = connection;
            _outgoingTransfers.Add(connection, transfer);
            SendRequestPdu(transfer, request);
        }

        private void SendRequestPdu(Transfer transfer, QuasiHttpRequestMessage request)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers
            };
            if (request.Body != null)
            {
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
                if (Transport.IsChunkDeliveryAcknowledged)
                {
                    transfer.OutgoingRequestBodyProtocol = new OutgoingAckedChunkTransferProtocol(this,
                        transfer, request.Body);
                }
                else
                {
                    if (request.Body is ByteBufferBody byteBufferBody && pdu.ContentLength <= Transport.MaximumChunkSize)
                    {
                        pdu.Data = byteBufferBody.Buffer;
                        pdu.DataOffset = byteBufferBody.Offset;
                        pdu.DataLength = byteBufferBody.ContentLength;
                    }
                    else
                    {
                        transfer.OutgoingRequestBodyProtocol = new OutgoingUnackedChunkTransferProtocol(this, transfer,
                            QuasiHttpPdu.PduTypeRequestChunkRet, request.Body);
                    }
                }
            }
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendRequestCancellationIndicator = cancellationIndicator;
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
            var pduBytes = pdu.Serialize();
            try
            {
                Transport.Write(transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
            }
            catch (Exception e)
            {
                AbortTransfer(transfer, e);
            }
        }

        public void ProcessRequestChunkGetPdu(object connection, QuasiHttpPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            transfer.OutgoingRequestBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
        }

        public void ProcessRequestFinPdu(object connection, QuasiHttpPdu _)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            ResetTimeout(transfer);
        }

        private void HandleSendRequestPduOutcome(Transfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
        }

        public void ProcessResponsePdu(object connection, QuasiHttpPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            if (transfer.IncomingResponseBodyProtocol != null)
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
            if (Transport.IsChunkDeliveryAcknowledged)
            {
                if (pdu.DataLength > 0)
                {
                    AbortTransfer(transfer, new Exception("acked chunked response protocol violation"));
                    return;
                }
                if (pdu.ContentLength != 0)
                {
                    transfer.IncomingResponseBodyProtocol = new IncomingAckedChunkTransferProtocol();
                    response.Body = transfer.IncomingResponseBodyProtocol.Body;
                }
            }
            else
            {
                if (pdu.DataLength > 0)
                {
                    response.Body = new ByteBufferBody(pdu.Data, pdu.DataOffset,
                        pdu.DataLength, pdu.ContentType, EventLoop);
                }
                else if (pdu.ContentLength != 0)
                {
                    transfer.IncomingResponseBodyProtocol = new IncomingUnackedChunkTransferProtocol(this, transfer,
                        QuasiHttpPdu.PduTypeResponseChunkGet, pdu.ContentLength,
                        pdu.ContentType);
                    response.Body = transfer.IncomingResponseBodyProtocol.Body;
                }
            }

            transfer.RequestCallback.Invoke(null, response);
            transfer.RequestCallback = null;

            if (transfer.IncomingResponseBodyProtocol == null)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
                ResetTimeout(transfer);
            }
        }

        public void ProcessResponseChunkRetPdu(object connection, QuasiHttpPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            transfer.IncomingResponseBodyProtocol.ProcessChunkRetPdu(pdu.Data, pdu.DataOffset, pdu.DataLength);
        }

        public void ResetTimeout(Transfer transfer)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        public void AbortTransfer(Transfer transfer, Exception e)
        {
            if (!_outgoingTransfers.Remove(transfer.Connection))
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

        private void DisableTransfer(Transfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.SendRequestCancellationIndicator?.Cancel();
            transfer.OutgoingRequestBodyProtocol?.Cancel(e);
            transfer.IncomingResponseBodyProtocol?.Cancel(e);
            Transport.ReleaseConnection(transfer.Connection);
            transfer.RequestCallback?.Invoke(e, null);
            transfer.RequestCallback = null;

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "outgoing transfer error");
            }
        }
    }
}
