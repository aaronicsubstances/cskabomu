using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.QuasiHttp
{
    internal class STCancellationIndicatorInternal
    {
        public void Cancel()
        {
            Cancelled = true;
        }

        public bool Cancelled { get; private set; }
    }
}
