using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ByteReceiveProtocol : ITransferProtocol
    {
        private IQuasiHttpBody _requestBody, _responseBody;

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
        }

        public void OnSend(IQuasiHttpRequestMessage request)
        {
            throw new NotImplementedException("implementation error");
        }

        public void OnReceive()
        {
            ProcessNewConnection();
        }

        public void OnReceiveMessage(byte[] data, int offset, int length)
        {
            throw new NotImplementedException("unsupported for byte-oriented transports");
        }

        private void ProcessNewConnection()
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
                        HandleRequestPduLengthReadOutcome(e, encodedLength);
                    }
                }, null);
            };
            ByteUtils.ReadBytesFully(Parent.Transport, Connection, encodedLength, 0, encodedLength.Length, cb);
        }

        private void HandleRequestPduLengthReadOutcome(Exception e, byte[] encodedLength)
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
                        HandleRequestPduReadOutcome(e, pduBytes);
                    }
                }, null);
            };
            ByteUtils.ReadBytesFully(Parent.Transport, Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleRequestPduReadOutcome(Exception e, byte[] pduBytes)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            var pdu = TransferPdu.Deserialize(pduBytes, 0, pduBytes.Length);
            switch (pdu.PduType)
            {
                case TransferPdu.PduTypeRequest:
                    ProcessRequestPdu(pdu);
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
            if (pdu.ContentLength != 0)
            {
                request.Body = new ByteOrientedTransferBody(pdu.ContentLength,
                    pdu.ContentType, Parent.Transport, Connection, Parent.EventLoop,
                    e => { });
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
                Parent.EventLoop.PostCallback(_ =>
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
                Headers = response.Headers,
                IncludeLengthPrefixDuringSerialization = true
            };
            
            _responseBody = response.Body;
            if (response.Body != null)
            {
                pdu.ContentLength = response.Body.ContentLength;
                pdu.ContentType = response.Body.ContentType;
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
                        HandleSendResponsePduOutcome(e, response);
                    }
                }, null);
            };
            var pduBytes = pdu.Serialize();
            Parent.Transport.WriteBytesOrSendMessage(Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendResponsePduOutcome(Exception e, IQuasiHttpResponseMessage response)
        {
            if (e != null)
            {
                Parent.AbortTransfer(this, e);
                return;
            }

            if (response.Body != null)
            {
                var cancellationIndicator = new STCancellationIndicator();
                ProcessingCancellationIndicator = cancellationIndicator;
                Action<Exception> cb = e2 =>
                {
                    Parent.EventLoop.PostCallback(_ =>
                    {
                        if (!cancellationIndicator.Cancelled)
                        {
                            cancellationIndicator.Cancel();
                            Parent.AbortTransfer(this, e2);
                        }
                    }, null);
                };
                ByteUtils.TransferBodyToTransport(Parent.Transport, Connection, response.Body, cb);
            }
            else
            {
                Parent.AbortTransfer(this, null);
            }
        }
    }
}
