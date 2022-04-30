using Kabomu.Common;
using Kabomu.QuasiHttp.Bodies;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ReceiveProtocol
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
            var request = new QuasiHttpRequestMessage
            {
                Path = pdu.Path,
                Headers = pdu.Headers
            };
            var transfer = new IncomingTransfer
            {
                RequestId = pdu.RequestId,
                Request = request,
                TimeoutMillis = DefaultTimeoutMillis,
                ReplyConnectionHandle = connectionHandle
            };
            if (pdu.ContentLength > 0)
            {
                if (pdu.EmbeddedBody != null)
                {
                    request.Body = new ByteBufferBody(pdu.EmbeddedBody, pdu.EmbeddedBodyOffset,
                        pdu.ContentLength, pdu.ContentType, EventLoop);
                }
                else
                {
                    request.Body = CreateNonChunkedRequestBody(pdu.RequestId, pdu.ContentLength, pdu.ContentType);
                    transfer.RequestBodyTransferRequired = true;
                }
            }
            else if (pdu.ContentLength < 0)
            {
                request.Body = CreateChunkedRequestBody(pdu.RequestId, pdu.ContentType);
                transfer.RequestBodyTransferRequired = true;
            }
            _incomingTransfers.Add(pdu.RequestId, transfer);
            ResetTimeout(transfer);
            BeginApplicationPipelineProcessing(transfer);
        }

        private IQuasiHttpBody CreateChunkedRequestBody(int requestId, string contentType)
        {
            throw new NotImplementedException();
        }

        private IQuasiHttpBody CreateNonChunkedRequestBody(int requestId, int contentLength, string contentType)
        {
            throw new NotImplementedException();
        }

        private void BeginApplicationPipelineProcessing(IncomingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.ApplicationProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, QuasiHttpResponseMessage> cb = (ex, res) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleApplicationProcessingOutcome(transfer, ex, res);
                    }
                }, null);
            };
            Application.ProcessRequest(transfer.Request, cb);
        }

        private void HandleApplicationProcessingOutcome(IncomingTransfer transfer, Exception error,
            QuasiHttpResponseMessage response)
        {            
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }

            if (response == null)
            {
                AbortTransfer(transfer, new Exception("null response"));
                return;
            }

            transfer.Response = response;
            SendResponsePdu(transfer, response);

            if (transfer.ResponseBodyTransferRequired)
            {
                ResetTimeout(transfer);
            }
            else
            {
                AbortTransfer(transfer, null);
            }
        }

        private void SendResponsePdu(IncomingTransfer transfer, QuasiHttpResponseMessage response)
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
                if (response.Body is ByteBufferBody byteBufferBody)
                {
                    // TODO: also check that size can fit into max pdu payload of 30KB.
                    pdu.EmbeddedBody = byteBufferBody.Buffer;
                    pdu.EmbeddedBodyOffset = byteBufferBody.Offset;
                }
                else
                {
                    transfer.ResponseBodyTransferRequired = true;
                }
            }
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            transfer.SendPduCancellationIndicator = cancellationIndicator;
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
            Transport.SendPdu(pduBytes, 0, pduBytes.Length, transfer.ReplyConnectionHandle, cb);
        }

        private void HandleSendResponsePduOutcome(IncomingTransfer transfer, Exception e)
        {
            if (e != null)
            {
                AbortTransfer(transfer, e);
            }
        }

        private void ResetTimeout(IncomingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        private void AbortTransfer(IncomingTransfer transfer, Exception exception)
        {
            if (!_incomingTransfers.Remove(transfer.RequestId))
            {
                return;
            }
            DisableTransfer(transfer, exception);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _incomingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _incomingTransfers.Clear();
        }

        private void DisableTransfer(IncomingTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = null;
            transfer.ApplicationProcessingCancellationIndicator?.Cancel();
            transfer.ApplicationProcessingCancellationIndicator = null;
            transfer.SendPduCancellationIndicator?.Cancel();
            transfer.SendPduCancellationIndicator = null;
            transfer.ReplyConnectionHandle = null;
            transfer.Response = null;
            transfer.Request = null;
        }
    }
}
