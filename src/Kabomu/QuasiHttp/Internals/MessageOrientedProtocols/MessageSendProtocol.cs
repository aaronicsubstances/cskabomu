using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal class MessageSendProtocol
    {
        private readonly Dictionary<object, MessageTransfer> _outgoingTransfers =
            new Dictionary<object, MessageTransfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ProcessOutgoingRequest(object remoteEndpoint, 
            QuasiHttpRequestMessage request,            
            QuasiHttpSendOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            var transfer = new MessageTransfer
            {
                SendCallback = cb,
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

        private void ProcessSendRequestDirectly(object remoteEndpoint, MessageTransfer transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, QuasiHttpResponseMessage> cb = (e, res) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleDirectSendRequestProcessingOutcome(e, res, transfer);
                    }
                }, null);
            };
            Transport.ProcessSendRequest(remoteEndpoint, request, cb);
        }

        private void HandleDirectSendRequestProcessingOutcome(Exception e, QuasiHttpResponseMessage res,
            MessageTransfer transfer)
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

            transfer.SendCallback.Invoke(e, res);
            transfer.SendCallback = null;
            DisableTransfer(transfer, null);
        }

        private void AllocateConnection(object remoteEndpoint, MessageTransfer transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, object> cb = (e, connection) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleConnectionAllocationOutcome(e, connection, transfer, request);
                    }
                }, null);
            };
            Transport.AllocateConnection(remoteEndpoint, cb);
        }

        private void HandleConnectionAllocationOutcome(Exception e, object connection, MessageTransfer transfer, 
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
            ResetTimeout(transfer);
        }

        private void SendRequestPdu(MessageTransfer transfer, QuasiHttpRequestMessage request)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers
            };
            if (request.Body != null)
            {
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
                bool bodyTransferRequired = true;
                if (request.Body is ByteBufferBody byteBufferBody)
                {
                    int sizeWithoutBody = pdu.Serialize().Length;
                    if (sizeWithoutBody + pdu.ContentLength <= Transport.MaxMessageSize)
                    {
                        pdu.Data = byteBufferBody.Buffer;
                        pdu.DataOffset = byteBufferBody.Offset;
                        pdu.DataLength = byteBufferBody.ContentLength;
                        bodyTransferRequired = false;
                    }
                }
                if (bodyTransferRequired)
                {
                    Action<Exception> abortCallback = e => AbortTransfer(transfer, e);
                    transfer.RequestBodyProtocol = new OutgoingChunkTransferProtocol(Transport, EventLoop,
                        transfer.Connection, abortCallback, TransferPdu.PduTypeRequestChunkRet, request.Body);
                }
            }
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
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
            Transport.WriteBytesOrSendMessage(transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendRequestPduOutcome(MessageTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
        }

        public void ProcessRequestChunkGetPdu(object connection, TransferPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            transfer.RequestBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
            ResetTimeout(transfer);
        }

        public void ProcessRequestFinPdu(object connection)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            ResetTimeout(transfer);
        }

        public void ProcessResponsePdu(object connection, TransferPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            if (transfer.ResponseBodyProtocol != null)
            {
                // ignore possible duplicate.
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
            else if (pdu.ContentLength != 0)
            {
                Action<Exception> abortCallback = e => AbortTransfer(transfer, e);
                transfer.ResponseBodyProtocol = new IncomingChunkTransferProtocol(Transport, EventLoop,
                    transfer.Connection, abortCallback, TransferPdu.PduTypeResponseChunkGet, pdu.ContentLength,
                    pdu.ContentType);
                response.Body = transfer.ResponseBodyProtocol.Body;
            }

            transfer.SendCallback.Invoke(null, response);
            transfer.SendCallback = null;

            if (transfer.ResponseBodyProtocol == null)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
                ResetTimeout(transfer);
            }
        }

        public void ProcessResponseChunkRetPdu(object connection, TransferPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            transfer.ResponseBodyProtocol.ProcessChunkRetPdu(pdu.Data, pdu.DataOffset, pdu.DataLength);
            ResetTimeout(transfer);
        }

        private void ResetTimeout(MessageTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        private void AbortTransfer(MessageTransfer transfer, Exception e)
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

        private void DisableTransfer(MessageTransfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();
            transfer.RequestBodyProtocol?.Cancel(e);
            transfer.ResponseBodyProtocol?.Cancel(e);
            transfer.SendCallback?.Invoke(e, null);
            transfer.SendCallback = null;

            if (transfer.Connection != null)
            {
                Transport.ReleaseConnection(transfer.Connection);
            }

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "outgoing transfer error");
            }
        }
    }
}
