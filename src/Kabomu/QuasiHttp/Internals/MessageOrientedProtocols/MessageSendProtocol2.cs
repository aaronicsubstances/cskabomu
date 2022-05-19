using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal class MessageSendProtocol2 : ITransferProtocol
    {
        private IQuasiHttpBody _requestBody, _responseBody;

        public IParentTransferProtocol Parent { get; set; }
        public object Connection { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public Action<Exception, QuasiHttpResponseMessage> SendCallback { get; set; }

        public void Cancel(Exception e)
        {
            _requestBody?.OnEndRead(e);
            _responseBody?.OnEndRead(e);
        }

        public void OnSend(QuasiHttpRequestMessage request)
        {
            throw new NotImplementedException();
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
                case TransferPdu.PduTypeRequestFin:
                    ProcessRequestFinPdu();
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void ProcessRequestChunkGetPdu(TransferPdu pdu)
        {
            throw new NotImplementedException();
        }

        private void ProcessRequestFinPdu()
        {
            throw new NotImplementedException();
        }

        private void ProcessResponsePdu(TransferPdu pdu)
        {
            throw new NotImplementedException();
        }

        private void ProcessResponseChunkRetPdu(TransferPdu pdu)
        {
            throw new NotImplementedException();
        }
    }
}
