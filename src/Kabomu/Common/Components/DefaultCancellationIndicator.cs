using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common.Components
{
    public class DefaultCancellationIndicator : ICancellationIndicator
    {
        private int _cancelled = 0;

        public void Cancel()
        {
            Interlocked.CompareExchange(ref _cancelled, 1, 0);
        }

        public bool Cancelled => _cancelled == 1;
    }
}
