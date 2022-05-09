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

        public void ProcessRequestPdu(object connection, QuasiHttpPdu pdu)
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
            if (Transport.IsChunkDeliveryAcknowledged)
            {
                if (pdu.DataLength > 0)
                {
                    AbortTransfer(transfer, new Exception("acked chunked request protocol violation"));
                    return;
                }
                if (pdu.ContentLength != 0)
                {
                    transfer.IncomingRequestBodyProtocol = new IncomingAckedChunkTransferProtocol();
                    request.Body = transfer.IncomingRequestBodyProtocol.Body;
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
                    transfer.IncomingRequestBodyProtocol = new IncomingUnackedChunkTransferProtocol(this, transfer,
                        QuasiHttpPdu.PduTypeRequestChunkGet, pdu.ContentLength, pdu.ContentType);
                    request.Body = transfer.IncomingRequestBodyProtocol.Body;
                }
            }
            BeginApplicationPipelineProcessing(transfer, request);
        }

        private void BeginApplicationPipelineProcessing(Transfer transfer, QuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ApplicationProcessingCancellationIndicator = cancellationIndicator;
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

        public void ProcessRequestChunkRetPdu(object connection, QuasiHttpPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            transfer.IncomingRequestBodyProtocol.ProcessChunkRetPdu(pdu.Data,
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

            if (transfer.OutgoingResponseBodyProtocol == null)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
                ResetTimeout(transfer);
            }
        }

        private void SendResponsePdu(Transfer transfer, QuasiHttpResponseMessage response)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeResponse,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers
            };

            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
                if (Transport.IsChunkDeliveryAcknowledged)
                {
                    transfer.OutgoingResponseBodyProtocol = new OutgoingAckedChunkTransferProtocol(this, transfer,
                        response.Body);
                }
                else
                {
                    if (response.Body is ByteBufferBody byteBufferBody && pdu.ContentLength <= Transport.MaximumChunkSize)
                    {
                        pdu.Data = byteBufferBody.Buffer;
                        pdu.DataOffset = byteBufferBody.Offset;
                        pdu.DataLength = byteBufferBody.ContentLength;
                    }
                    else
                    {
                        transfer.OutgoingResponseBodyProtocol = new OutgoingUnackedChunkTransferProtocol(this, transfer,
                            QuasiHttpPdu.PduTypeResponseChunkRet, response.Body);
                    }
                }
            }
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendResponseCancellationIndicator = cancellationIndicator;
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

        private void HandleSendPduOutcome(Transfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
        }

        public void ProcessResponseChunkGetPdu(object connection, QuasiHttpPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(connection))
            {
                return;
            }
            var transfer = _incomingTransfers[connection];
            transfer.OutgoingResponseBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
        }

        public void ProcessResponseFinPdu(object connection, QuasiHttpPdu pdu)
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
            transfer.ApplicationProcessingCancellationIndicator?.Cancel();
            transfer.SendResponseCancellationIndicator?.Cancel();
            transfer.IncomingRequestBodyProtocol?.Cancel(e);
            transfer.OutgoingResponseBodyProtocol?.Cancel(e);
            Transport.ReleaseConnection(transfer.Connection);

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "incoming transfer error");
            }
        }
    }
}
