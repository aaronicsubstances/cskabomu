using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents timeout API for instances of <see cref="StandardQuasiHttpClient"/>
    /// and <see cref="StandardQuasiHttpServer"/> classes to impose timeouts
    /// on request processing.
    /// </summary>
    /// <param name="proc">the procedure to run under timeout</param>
    /// <returns>a task whose result indicates whether a timeout occurred,
    /// and gives the return value of the function argument.</returns>
    public delegate Task<ITimeoutResult> CustomTimeoutScheduler(
        Func<Task<IQuasiHttpResponse>> proc);

    /// <summary>
    /// Represents result of using timeout API as represented by
    /// <see cref="CustomTimeoutScheduler"/> instances.
    /// </summary>
    public interface ITimeoutResult
    {
        /// <summary>
        /// Returns true or false depending on whether a timeout occurred
        /// or not respectively.
        /// </summary>
        bool Timeout { get; }

        /// <summary>
        /// Gets the value returned by the function argument to the
        /// timeout API represented by an instance of the
        /// <see cref="CustomTimeoutScheduler"/> delegate.
        /// </summary>
        IQuasiHttpResponse Response { get; }

        /// <summary>
        /// Gets any error which was thrown by function argument to the
        /// timeout API represented by an instance of the
        /// <see cref="CustomTimeoutScheduler"/> delegate.
        /// </summary>
        Exception Error { get; }
    }

    /// <summary>
    /// Provides default implementation of the <see cref="ITimeoutResult"/>
    /// interface, in which properties are mutable.
    /// </summary>
    public class DefaultTimeoutResult : ITimeoutResult
    {
        public bool Timeout { get; set; }

        public IQuasiHttpResponse Response { get; set; }

        public Exception Error { get; set; }
    }

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

    internal class DefaultCancellableTimeoutTaskInternal : ICancellableTimeoutTask
    {
        public Task<bool> Task { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public void Cancel()
        {
            CancellationTokenSource?.Cancel();
        }
    }
}
