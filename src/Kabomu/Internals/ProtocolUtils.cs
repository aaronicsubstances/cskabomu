using Kabomu.Common;
using System;

namespace Kabomu.Internals
{
    internal class ProtocolUtils
    {
        public static bool IsOperationPending(STCancellationIndicator cancellationIndicator)
        {
            return cancellationIndicator != null && !cancellationIndicator.Cancelled;
        }
    }
}