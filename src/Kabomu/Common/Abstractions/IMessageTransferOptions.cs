using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageTransferOptions
    {
        long MessageId { get; }
        bool ReceiveAlreadyStarted { get; }
        int TimeoutMillis { get; }
        ICancellationHandle CancellationHandle { get; }
    }
}
