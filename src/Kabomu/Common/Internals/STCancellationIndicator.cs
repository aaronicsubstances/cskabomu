using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common.Internals
{
    internal class STCancellationIndicator : ICancellationIndicator, IRecyclable
    {
        public void Cancel()
        {
            Cancelled = true;
        }

        public bool Cancelled { get; private set; }

        public int RecyclingFlags { get; set; }
    }
}
