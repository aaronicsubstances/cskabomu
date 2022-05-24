using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class MessageSendProtocol : ITransferProtocol
    {
        private IQuasiHttpBody _requestBody, _responseBody;
        private OutgoingChunkTransferProtocol _requestBodyProtocol;
        private IncomingChunkTransferProtocol _responseBodyProtocol;

        public IParentTransferProtocol Parent { get; set; }
        public object Connection { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public Action<Exception, IQuasiHttpResponseMessage> SendCallback { get; set; }

        public void Cancel(Exception e)
        {
            _requestBody?.OnEndRead(e);
            _responseBody?.OnEndRead(e);
            _requestBodyProtocol?.Cancel(e);
            _responseBodyProtocol?.Cancel(e);
        }

        public void OnSend(IQuasiHttpRequestMessage request)
        {
            SendRequestPdu(request);
        }

        public void OnReceive()
        {
            throw new NotImplementedException("implementation error");
        }

        public void OnReceiveMessage(byte[] data, int offset, int length)
        {
            var pdu = TransferPdu.Deserialize(data, offset, length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeResponse:
                    ProcessResponsePdu(pdu);
                    break;
                case TransferPdu.PduTypeRequestChunkGet:
                    ProcessRequestChunkGetPdu(pdu);
                    break;
                case TransferPdu.PduTypeResponseChunkRet:
                    ProcessResponseChunkRetPdu(pdu);
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void SendRequestPdu(IQuasiHttpRequestMessage request)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers
            };
            _requestBody = request.Body;
            if (request.Body != null)
            {
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
                bool bodyTransferRequired = true;
                if (request.Body is ByteBufferBody byteBufferBody)
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
                    _requestBodyProtocol = new OutgoingChunkTransferProtocol(Parent.Transport, Parent.EventLoop,
                        Connection, abortCallback, TransferPdu.PduTypeRequestChunkRet, request.Body);
                }
            }
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendRequestPduOutcome(e);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Parent.Transport.WriteBytesOrSendMessage(Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendRequestPduOutcome(Exception e)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }
        }

        private void ProcessRequestChunkGetPdu(TransferPdu pdu)
        {
            _requestBodyProtocol.ProcessChunkGetPdu(pdu.ContentLength);
        }

        private void ProcessResponsePdu(TransferPdu pdu)
        {

            var response = new DefaultQuasiHttpResponseMessage
            {
                StatusIndicatesSuccess = pdu.StatusIndicatesSuccess,
                StatusIndicatesClientError = pdu.StatusIndicatesClientError,
                StatusMessage = pdu.StatusMessage,
                Headers = pdu.Headers
            };
            if (pdu.DataLength > 0)
            {
                response.Body = new ByteBufferBody(pdu.Data, pdu.DataOffset,
                    pdu.DataLength, pdu.ContentType, Parent.EventLoop);
            }
            else if (pdu.ContentLength != 0)
            {
                Action<Exception> abortCallback = e => Parent.AbortTransfer(this, e);
                _responseBodyProtocol = new IncomingChunkTransferProtocol(Parent.Transport, Parent.EventLoop,
                    Connection, abortCallback, TransferPdu.PduTypeResponseChunkGet, pdu.ContentLength,
                    pdu.ContentType);
                response.Body = _responseBodyProtocol.Body;
            }
            _responseBody = response.Body;

            SendCallback.Invoke(null, response);
            SendCallback = null;

            if (_responseBodyProtocol == null)
            {
                Parent.AbortTransfer(this, null);
            }
        }

        private void ProcessResponseChunkRetPdu(TransferPdu pdu)
        {
            _responseBodyProtocol.ProcessChunkRetPdu(pdu.Data, pdu.DataOffset, pdu.DataLength);
        }
    }
}
