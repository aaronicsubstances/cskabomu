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

        private void ProcessSendRequestDirectly(object remoteEndpoint, Transfer transfer, QuasiHttpRequestMessage request)
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

            transfer.SendCallback.Invoke(e, res);
            transfer.SendCallback = null;
            DisableTransfer(transfer, null);
        }

        private void AllocateConnection(object remoteEndpoint, Transfer transfer, QuasiHttpRequestMessage request)
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
            ResetTimeout(transfer);
            SendRequestPdu(transfer, request);
        }

        private void SendRequestPdu(Transfer transfer, QuasiHttpRequestMessage request)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers,
                IncludeLengthPrefixDuringSerialization = Transport.IsByteOriented
            };
            if (request.Body != null)
            {
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
                transfer.RequestBodyTransferRequired = true;
                if (!Transport.IsByteOriented)
                {
                    if (request.Body is ByteBufferBody byteBufferBody)
                    {
                        int sizeWithoutBody = pdu.Serialize().Length;
                        if (sizeWithoutBody + pdu.ContentLength <= Transport.MaxMessageSize)
                        {
                            pdu.Data = byteBufferBody.Buffer;
                            pdu.DataOffset = byteBufferBody.Offset;
                            pdu.DataLength = byteBufferBody.ContentLength;
                            transfer.RequestBodyTransferRequired = false;
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
                        HandleSendRequestPduOutcome(transfer, e, request);
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

        public void ProcessRequestChunkGetPdu(object connection, TransferPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            transfer.MessageOrientedRequestBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
        }

        public void ProcessRequestFinPdu(object connection, TransferPdu _)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            ResetTimeout(transfer);
        }

        private void HandleSendRequestPduOutcome(Transfer transfer, Exception e, QuasiHttpRequestMessage request)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            if (transfer.RequestBodyTransferRequired)
            {
                if (Transport.IsByteOriented)
                {
                    ProtocolUtils.TransferBodyToTransport(Transport, transfer.Connection, request.Body, false);
                }
                else
                {
                    transfer.MessageOrientedRequestBodyProtocol = new OutgoingChunkTransferProtocol(this, transfer,
                        TransferPdu.PduTypeRequestChunkRet, request.Body);
                }
            }
            if (Transport.IsByteOriented)
            {
                ProcessResponsePduBytes(transfer);
            }
        }

        private void ProcessResponsePduBytes(Transfer transfer)
        {
            Console.WriteLine("got here response pdu begin");
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

        private void HandleResponsePduLengthReadOutcome(Transfer transfer, Exception e, byte[] encodedLength)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }

            int qHttpHeaderLen = ByteUtils.DeserializeInt32BigEndian(encodedLength, 0);
            Console.WriteLine("got here response pdu length = " + qHttpHeaderLen);
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

        private void HandleResponsePduReadOutcome(Transfer transfer, Exception e, byte[] pduBytes)
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
                    Console.WriteLine("got here response pdu success");
                    ProcessResponsePdu(transfer.Connection, pdu);
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        public void ProcessResponsePdu(object connection, TransferPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            if (transfer.ResponseBodyTransferRequired)
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
            if (Transport.IsByteOriented)
            {
                if (pdu.DataLength > 0)
                {
                    AbortTransfer(transfer, new Exception("byte oriented response transfer protocol violation"));
                    return;
                }
                if (pdu.ContentLength != 0)
                {
                    response.Body = new ByteOrientedTransferBody(true, pdu.ContentLength,
                        pdu.ContentType, Transport, transfer.Connection, EventLoop);
                    transfer.ResponseBodyTransferRequired = true;
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
                    transfer.MessageOrientedResponseBodyProtocol = new IncomingChunkTransferProtocol(this, transfer,
                        TransferPdu.PduTypeResponseChunkGet, pdu.ContentLength,
                        pdu.ContentType);
                    response.Body = transfer.MessageOrientedResponseBodyProtocol.Body;
                    transfer.ResponseBodyTransferRequired = true;
                }
            }

            transfer.SendCallback.Invoke(null, response);
            transfer.SendCallback = null;

            if (transfer.ResponseBodyTransferRequired)
            {
                if (Transport.IsByteOriented)
                {
                    // discard records of connection in QuasiHttpClient, but keep records
                    // in underlying transport.
                    transfer.Connection = null;
                    AbortTransfer(transfer, null);
                }
                else
                {
                    ResetTimeout(transfer);
                }
            }
            else
            {
                AbortTransfer(transfer, null);
            }
        }

        public void ProcessResponseChunkRetPdu(object connection, TransferPdu pdu)
        {
            if (!_outgoingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _outgoingTransfers[connection];
            transfer.MessageOrientedResponseBodyProtocol.ProcessChunkRetPdu(pdu.Data, pdu.DataOffset, pdu.DataLength);
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
            transfer.ProcessingCancellationIndicator?.Cancel();
            transfer.MessageOrientedRequestBodyProtocol?.Cancel(e);
            transfer.MessageOrientedResponseBodyProtocol?.Cancel(e);
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
