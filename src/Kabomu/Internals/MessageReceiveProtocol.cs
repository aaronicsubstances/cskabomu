using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class MessageReceiveProtocol : ITransferProtocol
    {
        private IQuasiHttpBody _requestBody, _responseBody;
        private IncomingChunkTransferProtocol _requestBodyProtocol;
        private OutgoingChunkTransferProtocol _responseBodyProtocol;

        public IParentTransferProtocol Parent { get; set; }
        public object Connection { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public Action<Exception, IQuasiHttpResponseMessage> SendCallback { get; set; }

        public void Cancel(Exception e)
        {
            _requestBody?.OnEndRead(Parent.Mutex, e);
            _responseBody?.OnEndRead(Parent.Mutex, e);
            _requestBodyProtocol?.Cancel(e);
            _responseBodyProtocol?.Cancel(e);
        }

        public void OnSend(IQuasiHttpRequestMessage request)
        {
            throw new NotImplementedException("implementation error");
        }

        public void OnReceive()
        {
            // nothing else to do.
        }

        public void OnReceiveMessage(byte[] data, int offset, int length)
        {
            var pdu = TransferPdu.Deserialize(data, offset, length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeRequest:
                    ProcessRequestPdu(pdu);
                    break;
                case TransferPdu.PduTypeRequestChunkRet:
                    ProcessRequestChunkRetPdu(pdu);
                    break;
                case TransferPdu.PduTypeResponseChunkGet:
                    ProcessResponseChunkGetPdu(pdu);
                    break;
                case TransferPdu.PduTypeFin:
                    ProcessFinPdu();
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void ProcessRequestPdu(TransferPdu pdu)
        {
            var request = new DefaultQuasiHttpRequestMessage
            {
                Path = pdu.Path,
                Headers = pdu.Headers
            };
            if (pdu.DataLength > 0)
            {
                request.Body = new ByteBufferBody(pdu.Data, pdu.DataOffset,
                    pdu.DataLength, pdu.ContentType);
            }
            else if (pdu.ContentLength != 0)
            {
                Action<Exception> abortCallback = e => Parent.AbortTransfer(this, e);
                _requestBodyProtocol = new IncomingChunkTransferProtocol(Parent.Transport, Parent.Mutex,
                    Connection, abortCallback, TransferPdu.PduTypeRequestChunkGet, pdu.ContentLength, pdu.ContentType);
                request.Body = _requestBodyProtocol.Body;
            }
            _requestBody = request.Body;
            BeginApplicationPipelineProcessing(request);
        }

        private void BeginApplicationPipelineProcessing(IQuasiHttpRequestMessage request)
        {
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception, IQuasiHttpResponseMessage> cb = (e, res) =>
            {
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleApplicationProcessingOutcome(e, res);
                    }
                }, null);
            };
            Parent.Application.ProcessRequest(request, cb);
        }

        private void HandleApplicationProcessingOutcome(Exception e, IQuasiHttpResponseMessage response)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (response == null)
            {
                Parent.AbortTransfer(this, new Exception("no response"));
                return;
            }

            SendResponsePdu(response);
        }

        private void SendResponsePdu(IQuasiHttpResponseMessage response)
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

            _responseBody = response.Body;
            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
                bool bodyTransferRequired = true;
                if (response.Body is ByteBufferBody byteBufferBody)
                {
                    int sizeWithoutBody = pdu.Serialize().Length;
                    if (sizeWithoutBody + pdu.ContentLength <= Parent.Transport.MaxMessageOrChunkSize)
                    {
                        pdu.Data = byteBufferBody.Buffer;
                        pdu.DataOffset = byteBufferBody.Offset;
                        pdu.DataLength = byteBufferBody.ContentLength;
                        bodyTransferRequired = false;
                    }
                }
                if (bodyTransferRequired)
                {
                    Action<Exception> abortCallback = e => Parent.AbortTransfer(this, e);
                    _responseBodyProtocol = new OutgoingChunkTransferProtocol(Parent.Transport, Parent.Mutex,
                        Connection, abortCallback, TransferPdu.PduTypeResponseChunkRet, response.Body);
                }
            }

            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendResponsePduOutcome(e);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Parent.Transport.WriteBytesOrSendMessage(Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendResponsePduOutcome(Exception e)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (_responseBodyProtocol == null)
            {
                Parent.AbortTransfer(this, null);
            }
        }

        private void ProcessRequestChunkRetPdu(TransferPdu pdu)
        {
            _requestBodyProtocol.ProcessChunkRetPdu(pdu.Data, pdu.DataOffset, pdu.DataLength);
        }

        private void ProcessResponseChunkGetPdu(TransferPdu pdu)
        {
            _responseBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
        }

        private void ProcessFinPdu()
        {
            Parent.AbortTransfer(this, null);
        }
    }
}
