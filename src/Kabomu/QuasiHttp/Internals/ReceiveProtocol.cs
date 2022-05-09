using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ReceiveProtocol : ITransferProtocol
    {
        private readonly Dictionary<int, IncomingTransfer> _incomingTransfers = 
            new Dictionary<int, IncomingTransfer>();

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }

        public void ProcessRequestPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            var transfer = new IncomingTransfer
            {
                TransferProtocol = this,
                RequestId = pdu.RequestId,
                TimeoutMillis = DefaultTimeoutMillis
            };
            _incomingTransfers.Add(pdu.RequestId, transfer);
            ResetTimeout(transfer);
            
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
            if (pdu.ContentLength != 0 && request.Body == null)
            {
                transfer.RequestBodyProtocol = new IncomingChunkTransferProtocol(this, transfer,
                    QuasiHttpPdu.PduTypeRequestChunkGet, pdu.ContentLength, pdu.ContentType,
                    connectionHandle);
                request.Body = transfer.RequestBodyProtocol.Body;
            }
            BeginApplicationPipelineProcessing(transfer, request, connectionHandle);
        }

        private void BeginApplicationPipelineProcessing(IncomingTransfer transfer, QuasiHttpRequestMessage request,
            object connectionHandle)
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
                        HandleApplicationProcessingOutcome(transfer, e, res, connectionHandle);
                    }
                }, null);
            };
            try
            {
                Application.ProcessRequest(request, cb);
            }
            catch (Exception e)
            {
                cancellationIndicator.Cancel();
                HandleApplicationProcessingOutcome(transfer, e, null, connectionHandle);
            }
        }

        public void ProcessRequestChunkRetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_incomingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _incomingTransfers[pdu.RequestId];
            transfer.RequestBodyProtocol.ProcessChunkRetPdu(pdu.Data,
                pdu.DataOffset, pdu.DataLength, connectionHandle);
        }

        private void HandleApplicationProcessingOutcome(IncomingTransfer transfer, Exception e,
            QuasiHttpResponseMessage response, object connectionHandle)
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

            SendResponsePdu(transfer, response, connectionHandle);

            if (transfer.ResponseBodyProtocol == null)
            {
                AbortTransfer(transfer, null);
            }
            else
            {
                ResetTimeout(transfer);
            }
        }

        private void SendResponsePdu(IncomingTransfer transfer, QuasiHttpResponseMessage response,
            object connectionHandle)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = QuasiHttpPdu.PduTypeResponse,
                RequestId = transfer.RequestId,
                StatusIndicatesSuccess = response.StatusIndicatesSuccess,
                StatusIndicatesClientError = response.StatusIndicatesClientError,
                StatusMessage = response.StatusMessage,
                Headers = response.Headers
            };

            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
                if (response.Body is ByteBufferBody byteBufferBody && pdu.ContentLength <= Transport.MaxChunkSize)
                {
                    pdu.Data = byteBufferBody.Buffer;
                    pdu.DataOffset = byteBufferBody.Offset;
                    pdu.DataLength = byteBufferBody.ContentLength;
                }
                else
                {
                    transfer.ResponseBodyProtocol = new OutgoingChunkTransferProtocol(this, transfer,
                        QuasiHttpPdu.PduTypeResponseChunkRet, response.Body);
                }
            }
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendResponseHeaderPduCancellationIndicator = cancellationIndicator;
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

        private void HandleSendPduOutcome(IncomingTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
                return;
            }
        }

        public void ProcessResponseChunkGetPdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            if (!_incomingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _incomingTransfers[pdu.RequestId];
            transfer.ResponseBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength, connectionHandle);
        }

        public void ProcessResponseFinPdu(QuasiHttpPdu pdu)
        {
            if (!_incomingTransfers.ContainsKey(pdu.RequestId))
            {
                return;
            }
            var transfer = _incomingTransfers[pdu.RequestId];
            AbortTransfer(transfer, null);
        }

        public void ResetTimeout(IncomingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        public void AbortTransfer(IncomingTransfer transfer, Exception e)
        {
            if (!_incomingTransfers.Remove(transfer.RequestId))
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

        private void DisableTransfer(IncomingTransfer transfer, Exception e)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.ApplicationProcessingCancellationIndicator?.Cancel();
            transfer.SendResponseHeaderPduCancellationIndicator?.Cancel();
            transfer.RequestBodyProtocol?.Cancel(e);
            transfer.ResponseBodyProtocol?.Cancel(e);

            if (e != null)
            {
                ErrorHandler?.Invoke(e, "incoming transfer error");
            }
        }
    }
}
