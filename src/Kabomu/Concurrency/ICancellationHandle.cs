using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Used to communicate cancellation requests in multithreaded execution contexts.
    /// </summary>
    /// <remarks>
    /// This interface provides a missing feature in <see cref="System.Threading.CancellationTokenSource"/>
    /// where a call to Cancel() is unable to atomically provide indication of whether cancellation has already been requested
    /// or not. Lock-free code depend on this atomicity, and that is the reason for the existence of this interface.
    /// </remarks>
    public interface ICancellationHandle
    {
        /// <summary>
        /// Returns whether or not the instance has been cancelled.
        /// </summary>
        bool IsCancelled { get; }

        /// <summary>
        /// Ensures IsCancelled property returns true subsequently after calling this method returns, and
        /// indicates whether cancellation was previously requested.
        /// </summary>
        /// <returns>false if Cancel() has been called before; true if this is the first time Cancel() is being called.</returns>
        bool Cancel();
    }
}
