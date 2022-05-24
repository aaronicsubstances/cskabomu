using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Internals
{
    internal class STCancellationIndicator
    {
        public void Cancel()
        {
            Cancelled = true;
        }

        public bool Cancelled { get; private set; }
    }
}
