using System;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ProtocolUtils
    {
        public static bool IsOperationPending(STCancellationIndicator cancellationIndicator)
        {
            return cancellationIndicator != null && !cancellationIndicator.Cancelled;
        }
    }
}