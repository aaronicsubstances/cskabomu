using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Abstractions
{
    public interface IMessageTransferOptions
    {
        long MessageIdPart1 { get; }
        long MessageIdPart2 { get; }
        bool MustExistAtRemotePeer { get; }
        int TimeoutMillis { get; }
        ICancellationHandle CancellationHandle { get; }
    }
}
