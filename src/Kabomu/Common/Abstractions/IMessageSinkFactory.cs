using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageSinkFactory
    {
        void CreateMessageSink(long msgIdPart1, long msgIdPart2,
            MessageSinkCreationCallback cb, object cbState);
    }
}
