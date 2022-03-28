using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageTransferOptions
    {
        int TimeoutMillis { get; }
        ICancellationIndicator CancellationIndicator { get; }
    }
}
