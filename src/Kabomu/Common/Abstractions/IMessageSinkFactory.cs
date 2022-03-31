using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageSinkFactory
    {
        void CreateMessageSink(MessageSinkCreationCallback cb, object cbState);
    }
}
