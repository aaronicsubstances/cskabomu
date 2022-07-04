using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.QuasiHttp.EntityBody
{
    public static class EntityBodyUtilsInternal
    {
        public static readonly Exception ReadCancellationException = new Exception("end of read");

        public static void TryCancelRead(CancellationTokenSource cts)
        {
            TryCancelRead(cts.IsCancellationRequested);
        }

        public static void TryCancelRead(bool endOfReadSeen)
        {
            if (endOfReadSeen)
            {
                throw ReadCancellationException;
            }
        }
    }
}
