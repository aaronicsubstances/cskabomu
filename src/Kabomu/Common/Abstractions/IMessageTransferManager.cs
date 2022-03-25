using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageTransferManager
    {
        IQpcFacility LowerLayer { get; }
        IMessageSinkFactory MessageSinkFactory { get; }
        void BeginReceive(IMessageSink msgSink, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState);
        void BeginSend(IMessageSource msgSource, IMessageTransferOptions options, 
            Action<object, Exception> cb, object cbState);
        void BeginReset(Exception causeOfReset, Action<object, Exception> cb, object cbState);
    }
}
