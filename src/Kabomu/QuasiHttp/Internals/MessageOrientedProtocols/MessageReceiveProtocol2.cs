using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal class MessageReceiveProtocol2 : ITransferProtocol
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
                case TransferPdu.PduTypeResponseFin:
                    ProcessResponseFinPdu();
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void ProcessRequestPdu(TransferPdu pdu)
        {
            throw new NotImplementedException();
        }

        private void ProcessRequestChunkRetPdu(TransferPdu pdu)
        {
            throw new NotImplementedException();
        }

        private void ProcessResponseChunkGetPdu(TransferPdu pdu)
        {
            throw new NotImplementedException();
        }

        private void ProcessResponseFinPdu()
        {
            throw new NotImplementedException();
        }
    }
}
