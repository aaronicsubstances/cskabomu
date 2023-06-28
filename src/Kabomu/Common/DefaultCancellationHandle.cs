using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Common
{
    /// <summary>
    /// Default implementation of <see cref="ICancellationHandle"/> interface used in Kabomu library,
    /// which leverages non-blocking synchronization to atomically determine whether cancellation requests have been made.
    /// </summary>
    public class DefaultCancellationHandle : ICancellationHandle
    {
        private int _cancelled = 0;

        /// <summary>
        /// Returns true or false if instance has been cancelled or not respectively.
        /// </summary>
        public bool IsCancelled => _cancelled != 0;

        /// <summary>
        /// Atomically cancels an instance of this class and also determines whether instance was already cancelled.
        /// </summary>
        /// <returns>false if Cancel() has been called before; true if this is the first time Cancel() is being called.</returns>
        public bool Cancel()
        {
            return Interlocked.CompareExchange(ref _cancelled, 1, 0) == 0;
        }
    }
}
