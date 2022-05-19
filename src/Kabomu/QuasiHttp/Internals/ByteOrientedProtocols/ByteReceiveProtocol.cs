using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.ByteOrientedProtocols
{
    internal class ByteReceiveProtocol
    {
        private readonly Dictionary<object, ByteTransfer> _incomingTransfers =
            new Dictionary<object, ByteTransfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }

        public void ProcessNewConnection(object connection)
        {
            var transfer = new ByteTransfer
            {
                Connection = connection,
                TimeoutMillis = DefaultTimeoutMillis
            };
            _incomingTransfers.Add(transfer.Connection, transfer);
            ResetTimeout(transfer);

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
                        HandleRequestPduLengthReadOutcome(transfer, e, encodedLength);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Transport, connection, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleRequestPduLengthReadOutcome(ByteTransfer transfer, Exception e, byte[] encodedLength)
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
                        HandleRequestPduReadOutcome(transfer, e, pduBytes);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Transport, transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleRequestPduReadOutcome(ByteTransfer transfer, Exception e, byte[] pduBytes)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            var pdu = TransferPdu.Deserialize(pduBytes, 0, pduBytes.Length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeRequest:
                    ProcessRequestPdu(transfer, pdu);
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void ProcessRequestPdu(ByteTransfer transfer, TransferPdu pdu)
        {
            var request = new QuasiHttpRequestMessage
            {
                Path = pdu.Path,
                Headers = pdu.Headers
            };
            if (pdu.ContentLength != 0)
            {
                var chunkedBody = new ByteOrientedTransferBody(pdu.ContentLength,
                    pdu.ContentType, Transport, transfer.Connection, EventLoop, null);
                request.Body = chunkedBody;
            }
            BeginApplicationPipelineProcessing(transfer, request);
        }

        private void BeginApplicationPipelineProcessing(ByteTransfer transfer, QuasiHttpRequestMessage request)
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
                        HandleApplicationProcessingOutcome(transfer, e, res);
                    }
                }, null);
            };
            Application.ProcessRequest(request, cb);
            ResetTimeout(transfer);
        }

        private void HandleApplicationProcessingOutcome(ByteTransfer transfer, Exception e,
            QuasiHttpResponseMessage response)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            if (response == null)
            {
                AbortTransfer(transfer, new Exception("no response"));
                return;
            }

            SendResponsePdu(transfer, response);
            ResetTimeout(transfer);
        }

        private void SendResponsePdu(ByteTransfer transfer, QuasiHttpResponseMessage response)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                IncludeLengthPrefixDuringSerialization = true
            };

            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
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
                        HandleSendResponsePduOutcome(transfer, e, response);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Transport.WriteBytesOrSendMessage(transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendResponsePduOutcome(ByteTransfer transfer, Exception e,
            QuasiHttpResponseMessage response)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
            if (response.Body != null)
            {
                ProtocolUtils.TransferBodyToTransport(Transport, transfer.Connection, response.Body, null);
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
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        public void AbortTransfer(ByteTransfer transfer, Exception e)
        {
            if (!_incomingTransfers.Remove(transfer.Connection))
            {
                return;
            }
            DisableTransfer(transfer, e);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _incomingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _incomingTransfers.Clear();
        }

        private void DisableTransfer(ByteTransfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();

            if (transfer.Connection != null)
            {
                Transport.ReleaseConnection(transfer.Connection);
            }

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "incoming transfer error");
            }
        }
    }
}
