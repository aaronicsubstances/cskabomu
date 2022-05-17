using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal class MessageReceiveProtocol
    {
        private readonly Dictionary<object, MessageTransfer> _incomingTransfers = 
            new Dictionary<object, MessageTransfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }

        public void ProcessNewConnection(object connection)
        {
            if (connection == null)
            {
                throw new ArgumentException("null connection");
            }
            var transfer = new MessageTransfer
            {
                Connection = connection,
                TimeoutMillis = DefaultTimeoutMillis
            };
            _incomingTransfers.Add(connection, transfer);
            ResetTimeout(transfer);
        }

        public void ProcessRequestPdu(object connection, TransferPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            var request = new QuasiHttpRequestMessage
            {
                Path = pdu.Path,
                Headers = pdu.Headers
            };
            if (pdu.DataLength > 0)
            {
                request.Body = new ByteBufferBody(pdu.Data, pdu.DataOffset,
                    pdu.DataLength, pdu.ContentType, EventLoop);
            }
            else if (pdu.ContentLength != 0)
            {
                Action<Exception> abortCallback = e => AbortTransfer(transfer, e);
                transfer.RequestBodyProtocol = new IncomingChunkTransferProtocol(Transport, EventLoop,
                    transfer.Connection, abortCallback, TransferPdu.PduTypeRequestChunkGet, pdu.ContentLength, pdu.ContentType);
                request.Body = transfer.RequestBodyProtocol.Body;
            }
            BeginApplicationPipelineProcessing(transfer, request);
        }

        private void BeginApplicationPipelineProcessing(MessageTransfer transfer, QuasiHttpRequestMessage request)
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

        public void ProcessRequestChunkRetPdu(object connection, TransferPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            transfer.RequestBodyProtocol.ProcessChunkRetPdu(pdu.Data,
                pdu.DataOffset, pdu.DataLength);
            ResetTimeout(transfer);
        }

        private void HandleApplicationProcessingOutcome(MessageTransfer transfer, Exception e,
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

        private void SendResponsePdu(MessageTransfer transfer, QuasiHttpResponseMessage response)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers
            };

            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
                bool bodyTransferRequired = true;
                if (response.Body is ByteBufferBody byteBufferBody)
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
                    transfer.ResponseBodyProtocol = new OutgoingChunkTransferProtocol(Transport, EventLoop,
                        transfer.Connection, abortCallback, TransferPdu.PduTypeResponseChunkRet, response.Body);
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
                        HandleSendResponsePduOutcome(transfer, e);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Transport.WriteBytesOrSendMessage(transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendResponsePduOutcome(MessageTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            if (transfer.ResponseBodyProtocol == null)
            {
                AbortTransfer(transfer, null);
            }
        }

        public void ProcessResponseChunkGetPdu(object connection, TransferPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            transfer.ResponseBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
            ResetTimeout(transfer);
        }

        public void ProcessResponseFinPdu(object connection)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            AbortTransfer(transfer, null);
        }

        private void ResetTimeout(MessageTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        private void AbortTransfer(MessageTransfer transfer, Exception e)
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

        private void DisableTransfer(MessageTransfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();
            transfer.RequestBodyProtocol?.Cancel(e);
            transfer.ResponseBodyProtocol?.Cancel(e);

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
