using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.QuasiHttp.EntityBody
{
    internal static class EntityBodyUtilsInternal
    {
        public static void ThrowIfReadCancelled(ICancellationHandle cancellationHandle)
        {
            ThrowIfReadCancelled(cancellationHandle.IsCancelled);
        }

        public static void ThrowIfReadCancelled(bool endOfReadSeen)
        {
            if (endOfReadSeen)
            {
                throw new EndOfReadException();
            }
        }
    }
}
