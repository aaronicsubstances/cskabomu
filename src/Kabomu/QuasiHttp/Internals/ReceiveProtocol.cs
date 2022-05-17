using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ReceiveProtocol : ITransferProtocol
    {
        private readonly Dictionary<object, Transfer> _incomingTransfers = 
            new Dictionary<object, Transfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }

        public void ProcessRequestPduBytes(object connection)
        {
            var transfer = new Transfer
            {
                Connection = connection,
                TimeoutMillis = DefaultTimeoutMillis
            };
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
               _ =>
               {
                   DisableTransfer(transfer, new Exception("connection accept timeout"));
               }, null);
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
                        HandleRequestPduLengthReadOutcome(connection, transfer, e, encodedLength);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Transport, connection, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleRequestPduLengthReadOutcome(object connection, Transfer transfer, Exception e, byte[] encodedLength)
        {
            if (e != null)
            {
                DisableTransfer(transfer, e);
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
                        HandleRequestPduReadOutcome(connection, transfer, e, pduBytes);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Transport, connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleRequestPduReadOutcome(object connection, Transfer transfer, Exception e, byte[] pduBytes)
        {
            if (e != null)
            {
                DisableTransfer(transfer, e);
                return;
            }

            var pdu = TransferPdu.Deserialize(pduBytes, 0, pduBytes.Length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeRequest:
                    ProcessRequestPdu(connection, pdu);
                    EventLoop.CancelTimeout(transfer.TimeoutId);
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        public void ProcessRequestPdu(object connection, TransferPdu pdu)
        {
            var transfer = new Transfer
            {
                Connection = connection,
                TimeoutMillis = DefaultTimeoutMillis
            };
            _incomingTransfers.Add(connection, transfer);
            ResetTimeout(transfer);
            
            var request = new QuasiHttpRequestMessage
            {
                Path = pdu.Path,
                Headers = pdu.Headers
            };
            if (Transport.IsByteOriented)
            {
                if (pdu.DataLength > 0)
                {
                    AbortTransfer(transfer, new Exception("byte oriented request transfer protocol violation"));
                    return;
                }
                if (pdu.ContentLength != 0)
                {
                    var chunkedBody = new ByteOrientedTransferBody(false, pdu.ContentLength,
                        pdu.ContentType, Transport, transfer.Connection, EventLoop);
                    request.Body = chunkedBody;
                }
            }
            else
            {
                if (pdu.DataLength > 0)
                {
                    request.Body = new ByteBufferBody(pdu.Data, pdu.DataOffset,
                        pdu.DataLength, pdu.ContentType, EventLoop);
                }
                else if (pdu.ContentLength != 0)
                {
                    transfer.MessageOrientedRequestBodyProtocol = new IncomingChunkTransferProtocol(this, transfer,
                        TransferPdu.PduTypeRequestChunkGet, pdu.ContentLength, pdu.ContentType);
                    request.Body = transfer.MessageOrientedRequestBodyProtocol.Body;
                }
            }
            BeginApplicationPipelineProcessing(transfer, request);
        }

        private void BeginApplicationPipelineProcessing(Transfer transfer, QuasiHttpRequestMessage request)
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
            try
            {
                Application.ProcessRequest(request, cb);
            }
            catch (Exception e)
            {
                AbortTransfer(transfer, e);
            }
        }

        public void ProcessRequestChunkRetPdu(object connection, TransferPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            transfer.MessageOrientedRequestBodyProtocol.ProcessChunkRetPdu(pdu.Data,
                pdu.DataOffset, pdu.DataLength);
        }

        private void HandleApplicationProcessingOutcome(Transfer transfer, Exception e,
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

        private void SendResponsePdu(Transfer transfer, QuasiHttpResponseMessage response)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeResponse,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers,
                IncludeLengthPrefixDuringSerialization = Transport.IsByteOriented
            };

            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
                transfer.ResponseBodyTransferRequired = true;
                if (!Transport.IsByteOriented)
                {
                    if (response.Body is ByteBufferBody byteBufferBody)
                    {
                        int sizeWithoutBody = pdu.Serialize().Length;
                        if (sizeWithoutBody + pdu.ContentLength <= Transport.MaxMessageSize)
                        {
                            pdu.Data = byteBufferBody.Buffer;
                            pdu.DataOffset = byteBufferBody.Offset;
                            pdu.DataLength = byteBufferBody.ContentLength;
                            transfer.ResponseBodyTransferRequired = false;
                        }
                    }
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
                        HandleSendResponsePduOutcome(transfer, e, response);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            try
            {
                Transport.WriteBytesOrSendMessage(transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
            }
            catch (Exception e)
            {
                AbortTransfer(transfer, e);
            }
        }

        private void HandleSendResponsePduOutcome(Transfer transfer, Exception e,
            QuasiHttpResponseMessage response)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
            Console.WriteLine("got here indicating response headers received by peer");
            if (transfer.ResponseBodyTransferRequired)
            {
                if (Transport.IsByteOriented)
                {
                    ProtocolUtils.TransferBodyToTransport(Transport, transfer.Connection, response.Body, true);
                    // discard records of connection in QuasiHttpClient, but keep records
                    // in underlying transport.
                    transfer.Connection = null;
                    AbortTransfer(transfer, null);
                }
                else
                {
                    transfer.MessageOrientedResponseBodyProtocol = new OutgoingChunkTransferProtocol(this, transfer,
                        TransferPdu.PduTypeResponseChunkRet, response.Body);
                }
            }
            else
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
            transfer.MessageOrientedResponseBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
        }

        public void ProcessResponseFinPdu(object connection, TransferPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            AbortTransfer(transfer, null);
        }

        public void ResetTimeout(Transfer transfer)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.TimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        public void AbortTransfer(Transfer transfer, Exception e)
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

        private void DisableTransfer(Transfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.TimeoutId);
            transfer.ProcessingCancellationIndicator?.Cancel();
            transfer.MessageOrientedRequestBodyProtocol?.Cancel(e);
            transfer.MessageOrientedResponseBodyProtocol?.Cancel(e);

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
