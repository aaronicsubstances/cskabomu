using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.ByteOrientedProtocols
{
    internal class ByteSendProtocol2 : ITransferProtocol
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
            SendRequestPdu(request);
        }

        public void OnReceive()
        {
            throw new NotImplementedException("implementation error");
        }

        public void OnReceiveMessage(byte[] data, int offset, int length)
        {
            throw new NotImplementedException("unsupported for byte-oriented transports");
        }

        private void SendRequestPdu(QuasiHttpRequestMessage request)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeRequest,
                Path = request.Path,
                Headers = request.Headers,
                IncludeLengthPrefixDuringSerialization = true
            };
            if (request.Body != null)
            {
                _requestBody = request.Body;
                pdu.ContentType = request.Body.ContentType;
                pdu.ContentLength = request.Body.ContentLength;
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
                        HandleSendRequestPduOutcome(e, request);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Parent.Transport.WriteBytesOrSendMessage(Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendRequestPduOutcome(Exception e, QuasiHttpRequestMessage request)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (request.Body != null)
            {
                ProtocolUtils.TransferBodyToTransport(Parent.Transport, Connection, request.Body,
                    e => { });
            }
            ProcessResponsePduBytes();
        }

        private void ProcessResponsePduBytes()
        {
            byte[] encodedLength = new byte[4];
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponsePduLengthReadOutcome(e, encodedLength);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Parent.Transport, Connection, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleResponsePduLengthReadOutcome(Exception e, byte[] encodedLength)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            int qHttpHeaderLen = ByteUtils.DeserializeInt32BigEndian(encodedLength, 0);
            var pduBytes = new byte[qHttpHeaderLen];
            var cancellationIndicator = new STCancellationIndicator();
            ProcessingCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                Parent.EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponsePduReadOutcome(e, pduBytes);
                    }
                }, null);
            };
            ProtocolUtils.ReadBytesFully(Parent.Transport, Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleResponsePduReadOutcome(Exception e, byte[] pduBytes)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            var pdu = TransferPdu.Deserialize(pduBytes, 0, pduBytes.Length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeResponse:
                    ProcessResponsePdu(pdu);
                    break;
                default:
                    throw new Exception("Unexpected pdu type: " + pdu.PduType);
            }
        }

        private void ProcessResponsePdu(TransferPdu pdu)
        {
            var response = new QuasiHttpResponseMessage
            {
                StatusIndicatesSuccess = pdu.StatusIndicatesSuccess,
                StatusIndicatesClientError = pdu.StatusIndicatesClientError,
                StatusMessage = pdu.StatusMessage,
                Headers = pdu.Headers
            };

            if (pdu.ContentLength != 0)
            {
                var cancellationIndicator = new STCancellationIndicator();
                ProcessingCancellationIndicator = cancellationIndicator;
                Action<Exception> cb = e =>
                {
                    Parent.EventLoop.PostCallback(_ =>
                    {
                        if (!cancellationIndicator.Cancelled)
                        {
                            cancellationIndicator.Cancel();
                            Parent.AbortTransfer(this, e);
                        }
                    }, null);
                };
                response.Body = new ByteOrientedTransferBody(pdu.ContentLength,
                    pdu.ContentType, Parent.Transport, Connection, Parent.EventLoop, cb);
                _responseBody = response.Body;
            }

            SendCallback.Invoke(null, response);
            SendCallback = null;

            if (response.Body == null)
            {
                Parent.AbortTransfer(this, null);
            }
        }
    }
}
