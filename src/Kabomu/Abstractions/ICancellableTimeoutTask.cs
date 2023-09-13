using System.Threading.Tasks;
using System.Threading;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents a task and a function to cancel it. Intended for use
    /// with timeouts.
    /// </summary>
    public interface ICancellableTimeoutTask
    {
        /// <summary>
        /// Gets the pending timeout task, which will complete
        /// with a result of true if timeout occurs.
        /// </summary>
        Task<bool> Task { get; }

        /// <summary>
        /// Cancels the pending timeout,such that the task
        /// property will complete with a result of false.
        /// </summary>
        void Cancel();
    }
}