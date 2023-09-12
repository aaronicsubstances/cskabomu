using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class CancellablePromise
    {
        public Task Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public void Cancel()
        {
            CancellationTokenSource?.Cancel();
        }
    }
}
