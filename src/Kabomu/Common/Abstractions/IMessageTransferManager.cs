using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageTransferManager
    {
        IQpcFacility QpcService { get; }
        IMessageSinkFactory MessageSinkFactory { get; }
        UncaughtErrorCallback ErrorHandler { get; set; }
        void BeginReceive(IMessageSink msgSink, long msgIdAtReceiver, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState);
        void BeginSend(object connectionHandle, IMessageSource msgSource, 
            IMessageTransferOptions options, 
            Action<object, Exception> cb, object cbState);
        void BeginSendStartedAtReceiver(object connectionHandle, IMessageSource msgSource, 
            long msgIdAtReceiver, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState);
        void BeginReset(Exception causeOfReset, Action<object, Exception> cb, object cbState);
        void OnReceivePdu(object connectionHandle, byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object fallbackPayload);
    }
}
