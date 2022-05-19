using Kabomu.Common;
using Kabomu.QuasiHttp.Internals;
using Kabomu.QuasiHttp.Internals.ByteOrientedProtocols;
using Kabomu.QuasiHttp.Internals.MessageOrientedProtocols;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpClient : IQuasiHttpClient
    {
        private readonly MessageSendProtocol _msgSendProtocol;
        private readonly ByteSendProtocol _byteSendProtocol;
        private readonly MessageReceiveProtocol _msgReceiveProtocol;
        private readonly ByteReceiveProtocol _byteReceiveProtocol;

        public DefaultQuasiHttpClient()
        {
            _msgSendProtocol = new MessageSendProtocol();
            _byteSendProtocol = new ByteSendProtocol();
            _msgReceiveProtocol = new MessageReceiveProtocol();
            _byteReceiveProtocol = new ByteReceiveProtocol();

            EventLoop = new DefaultEventLoopApi();
        }

        public IEventLoopApi EventLoop
        {
            get
            {
                return _msgSendProtocol.EventLoop;
            }
            set
            {
                _msgSendProtocol.EventLoop = value;
                _byteSendProtocol.EventLoop = value;
                _msgReceiveProtocol.EventLoop = value;
                _byteReceiveProtocol.EventLoop = value;
            }
        }

        public UncaughtErrorCallback ErrorHandler
        {
            get
            {
                return _msgSendProtocol.ErrorHandler;
            }
            set
            {
                _msgSendProtocol.ErrorHandler = value;
                _byteSendProtocol.ErrorHandler = value;
                _msgReceiveProtocol.ErrorHandler = value;
                _byteReceiveProtocol.ErrorHandler = value;
            }
        }

        public int DefaultTimeoutMillis
        {
            get
            {
                return _msgSendProtocol.DefaultTimeoutMillis;
            }
            set
            {
                _msgSendProtocol.DefaultTimeoutMillis = value;
                _byteSendProtocol.DefaultTimeoutMillis = value;
                _msgReceiveProtocol.DefaultTimeoutMillis = value;
                _byteReceiveProtocol.DefaultTimeoutMillis = value;
            }
        }

        public IQuasiHttpApplication Application
        {
            get
            {
                return _msgReceiveProtocol.Application;
            }
            set
            {
                _msgReceiveProtocol.Application = value;
                _byteReceiveProtocol.Application = value;
            }
        }

        public IQuasiHttpTransport Transport
        {
            get
            {
                return _msgSendProtocol.Transport;
            }
            set
            {
                _msgSendProtocol.Transport = value;
                _byteSendProtocol.Transport = value;
                _msgReceiveProtocol.Transport = value;
                _byteReceiveProtocol.Transport = value;
            }
        }

        public int MaxRetryPeriodMillis { get; set; }
        public int MaxRetryCount { get; set; }

        public void Send(object remoteEndpoint, QuasiHttpRequestMessage request,
            QuasiHttpSendOptions options, Action<Exception, QuasiHttpResponseMessage> cb)
        {
            EventLoop.PostCallback(_ =>
            {
                if (Transport.IsByteOriented)
                {
                    _byteSendProtocol.ProcessOutgoingRequest(remoteEndpoint, request,
                        options, cb);
                }
                else
                {
                    _msgSendProtocol.ProcessOutgoingRequest(remoteEndpoint, request,
                        options, cb);
                }
            }, null);
        }

        public void OnReceive(object connection)
        {
            EventLoop.PostCallback(_ =>
            {
                if (Transport.IsByteOriented)
                {
                    _byteReceiveProtocol.ProcessNewConnection(connection);
                }
                else
                {
                    _msgReceiveProtocol.ProcessNewConnection(connection);
                }
            }, null);
        }

        public void OnReceiveMessage(object connection, byte[] data, int offset, int length)
        {
            if (Transport.IsByteOriented)
            {
                throw new Exception("message processing forbidden on byte-oriented transports");
            }
            var pdu = TransferPdu.Deserialize(data, offset, length);
            EventLoop.PostCallback(_ =>
            {
                switch (pdu.PduType)
                {
                    case TransferPdu.PduTypeRequest:
                        _msgReceiveProtocol.ProcessRequestPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponse:
                        _msgSendProtocol.ProcessResponsePdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeRequestChunkGet:
                        _msgSendProtocol.ProcessRequestChunkGetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeRequestChunkRet:
                        _msgReceiveProtocol.ProcessRequestChunkRetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponseChunkGet:
                        _msgReceiveProtocol.ProcessResponseChunkGetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeResponseChunkRet:
                        _msgSendProtocol.ProcessResponseChunkRetPdu(connection, pdu);
                        break;
                    case TransferPdu.PduTypeRequestFin:
                        _msgSendProtocol.ProcessRequestFinPdu(connection);
                        break;
                    case TransferPdu.PduTypeResponseFin:
                        _msgReceiveProtocol.ProcessResponseFinPdu(connection);
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
                    _msgSendProtocol.ProcessReset(cause);
                    _byteSendProtocol.ProcessReset(cause);
                    _msgReceiveProtocol.ProcessReset(cause);
                    _byteReceiveProtocol.ProcessReset(cause);

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
