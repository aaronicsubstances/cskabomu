using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageSinkFactory
    {
        void CreateMessageSink(ITransferEndpoint remoteEndpoint, MessageSinkCreationCallback cb, object cbState);
    }
}
