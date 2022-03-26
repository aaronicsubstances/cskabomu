﻿using Kabomu.Common.Abstractions;
using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Components
{
    public class DefaultMessageTransferManager : IMessageTransferManager
    {
        private readonly ReceiveProtocol _receiveProtocol;
        private readonly SendProtocol _sendProtocol;

        private IQpcFacility _qpcService;
        private IMessageSinkFactory _messageSinkFactory;
        private int _defaultTimeoutMillis;
        private IEventLoopApi _eventLoop;
        private IRecyclingFactory _recyclingFactory;
        private IMessageIdGenerator _messageIdGenerator;

        public IQpcFacility QpcService
        {
            get
            {
                return _qpcService;
            }
            set
            {
                _qpcService = value;
                _receiveProtocol.QpcService = value;
                _sendProtocol.QpcService = value;
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

        public IRecyclingFactory RecyclingFactory
        {
            get
            {
                return _recyclingFactory;
            }
            set
            {
                _recyclingFactory = value;
                _receiveProtocol.RecyclingFactory = value;
                _sendProtocol.RecyclingFactory = value;
            }
        }

        public IMessageIdGenerator MessageIdGenerator
        {
            get
            {
                return _messageIdGenerator;
            }
            set
            {
                _messageIdGenerator = value;
                _sendProtocol.MessageIdGenerator = value;
            }
        }

        public DefaultMessageTransferManager()
        {
            _receiveProtocol = new ReceiveProtocol();
            _sendProtocol = new SendProtocol();
        }

        public void BeginReceive(IMessageSink msgSink, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState)
        {
            _receiveProtocol.BeginReceive(msgSink, options, cb, cbState);
        }

        public void BeginSend(IMessageSource msgSource, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState)
        {
            _sendProtocol.BeginSend(msgSource, options, cb, cbState);
        }

        public void BeginReset(Exception causeOfReset, Action<object, Exception> cb, object cbState)
        {
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

        public void OnReceivePdu(byte version, byte pduType, byte flags, byte errorCode, long messageId, 
            byte[] data, int offset, int length, object alternativePayload)
        {
            EventLoop.PostCallback(_ =>
            {
                switch (pduType)
                {
                    case DefaultProtocolDataUnit.PduTypeData:
                        _receiveProtocol.OnReceiveDataPdu(flags, messageId, data, offset, length, alternativePayload);
                        break;
                    case DefaultProtocolDataUnit.PduTypeDataAck:
                        _sendProtocol.OnReceiveDataAckPdu(messageId, errorCode);
                        break;
                    default:
                        throw new Exception("unexpected pdu type: " + pduType);
                }
            }, null);
        }
    }
}
