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

        public void Send(QuasiHttpRequestMessage request, object connectionHandleOrRemoteEndpoint,
            QuasiHttpSendOptions options, Action<Exception, QuasiHttpResponseMessage> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                _sendProtocol.ProcessOutgoingRequest(request, connectionHandleOrRemoteEndpoint,
                    options, cb);
            }, null);
        }

        public void ReceivePdu(QuasiHttpPdu pdu, object connectionHandle)
        {
            EventLoop.PostCallback(_ =>
            {
                switch (pdu.PduType)
                {
                    case QuasiHttpPdu.PduTypeRequest:
                        _receiveProtocol.ProcessRequestPdu(pdu, connectionHandle);
                        break;
                    case QuasiHttpPdu.PduTypeResponse:
                        _sendProtocol.ProcessResponsePdu(pdu, connectionHandle);
                        break;
                    case QuasiHttpPdu.PduTypeRequestChunkGet:
                        _sendProtocol.ProcessRequestChunkGetPdu(pdu, connectionHandle);
                        break;
                    case QuasiHttpPdu.PduTypeRequestChunkRet:
                        _receiveProtocol.ProcessRequestChunkRetPdu(pdu, connectionHandle);
                        break;
                    case QuasiHttpPdu.PduTypeResponseChunkGet:
                        _receiveProtocol.ProcessResponseChunkGetPdu(pdu, connectionHandle);
                        break;
                    case QuasiHttpPdu.PduTypeResponseChunkRet:
                        _sendProtocol.ProcessResponseChunkRetPdu(pdu, connectionHandle);
                        break;
                    case QuasiHttpPdu.PduTypeRequestFin:
                        _sendProtocol.ProcessRequestFinPdu(pdu);
                        break;
                    case QuasiHttpPdu.PduTypeResponseFin:
                        _receiveProtocol.ProcessResponseFinPdu(pdu);
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
                _sendProtocol.ProcessReset(cause);
                _receiveProtocol.ProcessReset(cause);
            }, null);
        }
    }
}
