using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.ByteOrientedProtocols
{
    internal class ByteSendProtocol
    {
        private readonly Dictionary<object, ByteTransfer> _outgoingTransfers =
            new Dictionary<object, ByteTransfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ProcessOutgoingRequest(object remoteEndpoint,
            QuasiHttpRequestMessage request,
            QuasiHttpSendOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            var transfer = new ByteTransfer
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

        private void ProcessSendRequestDirectly(object remoteEndpoint, ByteTransfer transfer, QuasiHttpRequestMessage request)
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
            ByteTransfer transfer)
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

        private void AllocateConnection(object remoteEndpoint, ByteTransfer transfer, QuasiHttpRequestMessage request)
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

        private void HandleConnectionAllocationOutcome(Exception e, object connection, ByteTransfer transfer,
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

        private void SendRequestPdu(ByteTransfer transfer, QuasiHttpRequestMessage request)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers,
                IncludeLengthPrefixDuringSerialization = true
            };
            if (request.Body != null)
            {
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
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
                        HandleSendRequestPduOutcome(transfer, e, request);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Transport.WriteBytesOrSendMessage(transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendRequestPduOutcome(ByteTransfer transfer, Exception e, QuasiHttpRequestMessage request)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            if (request.Body != null)
            {
                ProtocolUtils.TransferBodyToTransport(Transport, transfer.Connection, request.Body, null);
            }
            ProcessResponsePduBytes(transfer);
        }

        private void ProcessResponsePduBytes(ByteTransfer transfer)
        {
            byte[] encodedLength = new byte[4];
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponsePduLengthReadOutcome(transfer, e, encodedLength);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Transport, transfer.Connection, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleResponsePduLengthReadOutcome(ByteTransfer transfer, Exception e, byte[] encodedLength)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            int qHttpHeaderLen = ByteUtils.DeserializeInt32BigEndian(encodedLength, 0);
            var pduBytes = new byte[qHttpHeaderLen];
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponsePduReadOutcome(transfer, e, pduBytes);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Transport, transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleResponsePduReadOutcome(ByteTransfer transfer, Exception e, byte[] pduBytes)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            var pdu = TransferPdu.Deserialize(pduBytes, 0, pduBytes.Length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeResponse:
                    ProcessResponsePdu(transfer, pdu);
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void ProcessResponsePdu(ByteTransfer transfer, TransferPdu pdu)
        {
            var response = new QuasiHttpResponseMessage
            {
                StatusIndicatesSuccess = pdu.StatusIndicatesSuccess,
                StatusIndicatesClientError = pdu.StatusIndicatesClientError,
                StatusMessage = pdu.StatusMessage,
                Headers = pdu.Headers
            };
            
            if (pdu.ContentLength != 0)
            {
                response.Body = new ByteOrientedTransferBody(pdu.ContentLength,
                    pdu.ContentType, Transport, transfer.Connection, EventLoop, null);
            }

            transfer.SendCallback.Invoke(null, response);
            transfer.SendCallback = null;

            if (response.Body != null)
            {
                // discard records of connection in QuasiHttpClient, but keep records
                // in underlying transport.
                transfer.Connection = null;
            }
            AbortTransfer(transfer, null);
        }

        public void ResetTimeout(ByteTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        public void AbortTransfer(ByteTransfer transfer, Exception e)
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

        private void DisableTransfer(ByteTransfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();
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
