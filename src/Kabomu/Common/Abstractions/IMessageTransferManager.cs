using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageTransferManager
    {
        IQpcFacility QpcService { get; }
        IMessageSinkFactory MessageSinkFactory { get; }
        long BeginReceive(IMessageSink msgSink, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState);
        long BeginSend(IMessageSource msgSource, IMessageTransferOptions options, 
            Action<object, Exception> cb, object cbState);
        void BeginSendStartedAtReceiver(IMessageSource msgSource, long msgIdAtReceiver, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState);
        void BeginReset(Exception causeOfReset, Action<object, Exception> cb, object cbState);
        void OnReceivePdu(byte version, byte pduType, byte flags, byte errorCode,
            long messageId, byte[] data, int offset, int length, object additionalPayload);
    }
}
