using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Implementation of cancellation handling used in Kabomu library, which leverages non-blocking synchronization
    /// to determine whether cancellation requests have been made.
    /// </summary>
    public class DefaultCancellationHandle : ICancellationHandle
    {
        private int _cancelled = 0;

        /// <inheritdoc />
        public bool IsCancelled => _cancelled != 0;

        /// <inheritdoc />
        public bool Cancel()
        {
            return Interlocked.CompareExchange(ref _cancelled, 1, 0) == 0;
        }
    }
}
