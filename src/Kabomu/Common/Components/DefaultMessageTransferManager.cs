using Kabomu.Common.Abstractions;
using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("Kabomu.Tests")]

namespace Kabomu.Common.Components
{
    public class DefaultMessageTransferManager : IMessageTransferManager
    {
        public static readonly byte ErrorCodeGeneral = 1;
        public static readonly byte ErrorCodeProtocolViolation = 2;
        public static readonly byte ErrorCodeCancelled = 3;
        public static readonly byte ErrorCodeReset = 4;
        public static readonly byte ErrorCodeSendTimeout = 5;
        public static readonly byte ErrorCodeReceiveTimeout = 6;
        public static readonly byte ErrorCodeAbortedBySender = 7;
        public static readonly byte ErrorCodeAbortedByReceiver = 8;
        public static readonly byte ErrorCodeMessageIdNotFound = 9;
        public static readonly byte ErrorCodeMessageIdInUse = 10;

        private static readonly string[] ErrorMessages;

        static DefaultMessageTransferManager()
        {
            ErrorMessages = new string[20];
            SetErrorMessage(ErrorCodeGeneral, "General Error");
            SetErrorMessage(ErrorCodeProtocolViolation, "Protocol Violation");
            SetErrorMessage(ErrorCodeCancelled, "Cancelled");
            SetErrorMessage(ErrorCodeReset, "Reset");
            SetErrorMessage(ErrorCodeSendTimeout, "Send Timeout");
            SetErrorMessage(ErrorCodeReceiveTimeout, "Receive Timeout");
            SetErrorMessage(ErrorCodeAbortedBySender, "Aborted by Sender");
            SetErrorMessage(ErrorCodeAbortedByReceiver, "Aborted by Receiver");
            SetErrorMessage(ErrorCodeMessageIdNotFound, "Message Id Not Found");
            SetErrorMessage(ErrorCodeMessageIdInUse, "Message Id In Use");
        }

        private static void SetErrorMessage(byte errorCode, string message)
        {
            var prev = ErrorMessages[errorCode];
            if (prev != null)
            {
                throw new Exception("duplicate error message for error code " + errorCode + ": " +
                    $"{prev} vrs {message}");
            }
            ErrorMessages[errorCode] = message;
        }

        public static string GenerateErrorMessage(int errorCode, string fallback)
        {
            string errorMessage = null;
            if (errorCode >= 0 && errorCode < ErrorMessages.Length)
            {
                errorMessage = errorCode + ":" + (ErrorMessages[errorCode] ?? "Reserved");
            }
            return errorMessage ?? fallback ?? $"{errorCode}:N/A";
        }

        private IQuasiHttpTransport _transport;
        private IMessageSinkFactory _messageSinkFactory;
        private int _defaultTimeoutMillis;
        private IEventLoopApi _eventLoop;
        private UncaughtErrorCallback _errorHandler;

        private readonly ReceiveProtocol _receiveProtocol;
        private readonly SendProtocol _sendProtocol;

        public DefaultMessageTransferManager()
        {
            _receiveProtocol = new ReceiveProtocol();
            _sendProtocol = new SendProtocol();
        }

        public IQuasiHttpTransport QuasiHttpTransport
        {
            get
            {
                return _transport;
            }
            set
            {
                _transport = value;
                _receiveProtocol.QuasiHttpTransport = value;
                _sendProtocol.QuasiHttpTransport = value;
            }
        }

        public IMessageSinkFactory MessageSinkFactory
        {
            get
            {
                return _messageSinkFactory;
            }
            set
            {
                _messageSinkFactory = value;
                _receiveProtocol.MessageSinkFactory = value;
            }
        }

        public int DefaultTimeoutMillis
        {
            get
            {
                return _defaultTimeoutMillis;
            }
            set
            {
                _defaultTimeoutMillis = value;
                _receiveProtocol.DefaultTimeoutMillis = value;
                _sendProtocol.DefaultTimeoutMillis = value;
            }
        }

        public IEventLoopApi EventLoop
        {
            get
            {
                return _eventLoop;
            }
            set
            {
                _eventLoop = value;
                _receiveProtocol.EventLoop = value;
                _sendProtocol.EventLoop = value;
            }
        }

        public UncaughtErrorCallback ErrorHandler
        {
            get
            {
                return _errorHandler;
            }
            set
            {
                _errorHandler = value;
                _receiveProtocol.ErrorHandler = value;
                _sendProtocol.ErrorHandler = value;
            }
        }

        public void BeginReceive(IMessageSink msgSink, long msgIdAtReceiver, 
            IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState)
        {
            _receiveProtocol.BeginReceive(msgSink, msgIdAtReceiver, options, cb, cbState);
        }

        public void BeginSend(object connectionHandle, IMessageSource msgSource, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState)
        {
            _sendProtocol.BeginSend(connectionHandle, msgSource, options, cb, cbState);
        }

        public void BeginSendStartedAtReceiver(object connectionHandle, IMessageSource msgSource,
            long msgIdAtReceiver, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState)
        {
            _sendProtocol.BeginSendStartedAtReceiver(connectionHandle, msgSource, msgIdAtReceiver, options, cb, cbState);
        }

        public void BeginReset(Exception causeOfReset, Action<object, Exception> cb, object cbState)
        {
            if (causeOfReset == null)
            {
                causeOfReset = new Exception(GenerateErrorMessage(ErrorCodeReset, null));
            }
            EventLoop.PostCallback(_ =>
            {
                try
                {
                    _sendProtocol.ProcessReset(causeOfReset);
                    _receiveProtocol.ProcessReset(causeOfReset);
                    cb.Invoke(cbState, null);
                }
                catch (Exception ex)
                {
                    cb.Invoke(cbState, ex);
                }
            }, null);
        }

        public void OnReceivePdu(object connectionHandle, byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object fallbackPayload)
        {
            EventLoop.PostCallback(_ =>
            {
                switch (pduType)
                {
                    case DefaultProtocolDataUnit.PduTypeFirstChunk:
                        _receiveProtocol.OnReceiveFirstChunk(connectionHandle, flags, messageId,
                            data, offset, length, fallbackPayload);
                        break;
                    case DefaultProtocolDataUnit.PduTypeSubsequentChunk:
                        _receiveProtocol.OnReceiveSubsequentChunk(connectionHandle, flags, messageId,
                            data, offset, length, fallbackPayload);
                        break;
                    case DefaultProtocolDataUnit.PduTypeFirstChunkAck:
                        _sendProtocol.OnReceiveFirstChunkAck(connectionHandle, flags, messageId, errorCode);
                        break;
                    case DefaultProtocolDataUnit.PduTypeSubsequentChunkAck:
                        _sendProtocol.OnReceiveSubsequentChunkAck(connectionHandle, flags, messageId, errorCode);
                        break;
                    default:
                        throw new Exception("unexpected pdu type: " + pduType);
                }
            }, null);
        }
    }
}
