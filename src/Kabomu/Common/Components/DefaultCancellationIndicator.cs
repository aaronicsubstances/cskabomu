using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common.Components
{
    public class DefaultCancellationIndicator : ICancellationIndicator
    {
        public DefaultCancellationIndicator(CancellationToken ct)
        {
            Ct = ct;
        }

        private CancellationToken Ct { get; }

        public bool Cancelled => Ct.IsCancellationRequested;
    }
}
