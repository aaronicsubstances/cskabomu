using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ByteSendProtocol : ITransferProtocol
    {
        private IQuasiHttpBody _requestBody, _responseBody;

        public IParentTransferProtocol Parent { get; set; }
        public object Connection { get; set; }
        public STCancellationIndicator ProcessingCancellationIndicator { get; set; }
        public int TimeoutMillis { get; set; }
        public object TimeoutId { get; set; }
        public Action<Exception, IQuasiHttpResponse> SendCallback { get; set; }

        public void Cancel(Exception e)
        {
            _requestBody?.OnEndRead(Parent.Mutex, e);
            _responseBody?.OnEndRead(Parent.Mutex, e);
        }

        public void OnSend(IQuasiHttpRequest request)
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

        private void SendRequestPdu(IQuasiHttpRequest request)
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
                        HandleSendRequestPduOutcome(e, request);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Parent.Transport.WriteBytes(Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendRequestPduOutcome(Exception e, IQuasiHttpRequest request)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (request.Body != null)
            {
                Parent.TransferBodyToTransport(Connection, request.Body, e => { });
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
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponsePduLengthReadOutcome(e, encodedLength);
                    }
                }, null);
            };
            Parent.ReadBytesFullyFromTransport(Connection, encodedLength, 0, encodedLength.Length, cb);
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
                Parent.Mutex.RunExclusively(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleResponsePduReadOutcome(e, pduBytes);
                    }
                }, null);
            };
            Parent.ReadBytesFullyFromTransport(Connection, pduBytes, 0, pduBytes.Length, cb);
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
            var response = new DefaultQuasiHttpResponse
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
                Action closeCb = () =>
                {
                    Parent.Mutex.RunExclusively(_ =>
                    {
                        if (!cancellationIndicator.Cancelled)
                        {
                            cancellationIndicator.Cancel();
                            Parent.AbortTransfer(this, null);
                        }
                    }, null);
                };
                response.Body = new ByteOrientedTransferBody(pdu.ContentLength,
                    pdu.ContentType, Parent.Transport, Connection, closeCb);
            }
            _responseBody = response.Body;

            SendCallback.Invoke(null, response);
            SendCallback = null;

            if (response.Body == null)
            {
                Parent.AbortTransfer(this, null);
            }
        }
    }
}
