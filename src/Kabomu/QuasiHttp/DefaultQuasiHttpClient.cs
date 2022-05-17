using Kabomu.Common;
using Kabomu.QuasiHttp.Internals;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpClient : IQuasiHttpClient
    {
        private readonly SendProtocol _sendProtocol;
        private readonly ReceiveProtocol _receiveProtocol;

        public DefaultQuasiHttpClient()
        {
            _sendProtocol = new SendProtocol();
            _receiveProtocol = new ReceiveProtocol();
            EventLoop = new DefaultEventLoopApi();
        }

        public IEventLoopApi EventLoop
        {
            get
            {
                return _sendProtocol.EventLoop;
            }
            set
            {
                _sendProtocol.EventLoop = value;
                _receiveProtocol.EventLoop = value;
            }
        }

        public UncaughtErrorCallback ErrorHandler
        {
            get
            {
                return _sendProtocol.ErrorHandler;
            }
            set
            {
                _sendProtocol.ErrorHandler = value;
                _receiveProtocol.ErrorHandler = value;
            }
        }

        public int DefaultTimeoutMillis
        {
            get
            {
                return _sendProtocol.DefaultTimeoutMillis;
            }
            set
            {
                _sendProtocol.DefaultTimeoutMillis = value;
                _receiveProtocol.DefaultTimeoutMillis = value;
            }
        }

        public IQuasiHttpApplication Application
        {
            get
            {
                return _receiveProtocol.Application;
            }
            set
            {
                _receiveProtocol.Application = value;
            }
        }

        public IQuasiHttpTransport Transport
        {
            get
            {
                return _sendProtocol.Transport;
            }
            set
            {
                _sendProtocol.Transport = value;
                _receiveProtocol.Transport = value;
            }
        }

        public int MaxRetryPeriodMillis { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int MaxRetryCount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Send(object remoteEndpoint, QuasiHttpRequestMessage request,
            QuasiHttpSendOptions options, Action<Exception, QuasiHttpResponseMessage> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                _sendProtocol.ProcessOutgoingRequest(remoteEndpoint, request,
                    options, cb);
            }, null);
        }

        public void OnReceiveBytes(object connection)
        {
            EventLoop.PostCallback(_ =>
            {
                _receiveProtocol.ProcessRequestPduBytes(connection);
            }, null);
        }

        public void OnReceiveMessage(object connection, byte[] data, int offset, int length)
        {
            var pdu = TransferPdu.Deserialize(data, offset, length);
            EventLoop.PostCallback(_ =>
            {
                switch (pdu.PduType)
                {
                    case TransferPdu.PduTypeRequest:
                        _receiveProtocol.ProcessRequestPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponse:
                        _sendProtocol.ProcessResponsePdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeRequestChunkGet:
                        _sendProtocol.ProcessRequestChunkGetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeRequestChunkRet:
                        _receiveProtocol.ProcessRequestChunkRetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponseChunkGet:
                        _receiveProtocol.ProcessResponseChunkGetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponseChunkRet:
                        _sendProtocol.ProcessResponseChunkRetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeRequestFin:
                        _sendProtocol.ProcessRequestFinPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponseFin:
                        _receiveProtocol.ProcessResponseFinPdu(connection, pdu);
                        break;
                    default:
                        throw new Exception("Unknown pdu type: " + pdu.PduType);
                }
            }, null);
        }

        public void Reset(Exception cause, Action<Exception> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                try
                {
                    _sendProtocol.ProcessReset(cause);
                    _receiveProtocol.ProcessReset(cause);
                    cb.Invoke(null);
                }
                catch (Exception e)
                {
                    cb.Invoke(e);
                }
            }, null);
        }
    }
}
