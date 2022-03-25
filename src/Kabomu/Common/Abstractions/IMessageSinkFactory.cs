using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageSinkFactory
    {
        void CreateMessageSink(long msgId, MessageSinkCreationCallback cb, object cbState);
    }
}
