using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides default implementation of timer API which runs callbacks without any mutual exclusion.
    /// </summary>
    public class DefaultTimerApi : ITimerApi
    {
        /// <summary>
        /// Schedules a callback to execute after a given time wait period if not cancelled.
        /// </summary>
        /// <param name="cb">the callback to execute after some time</param>
        /// <param name="millis">wait time period before execution in milliseconds.</param>
        /// <returns>a cancellation handle object which can be passed to <see cref="ClearTimeout"/>
        /// to cancel the execution and cause the callback to never run.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="millis"/> argument is negative.</exception>
        public object SetTimeout(Action cb, int millis)
        {
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            if (millis < 0)
            {
                throw new ArgumentException("negative timeout value: " + millis);
            }
            var cancellationHandle = new CancellationTokenSource();
            Task.Delay(millis, cancellationHandle.Token).ContinueWith(t =>
            {
                if (t.IsCanceled)
                {
                    return;
                }
                cb.Invoke();
            });
            return new SetTimeoutCancellationHandle
            {
                Cts = cancellationHandle
            };
        }

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetTimeout"/>.
        /// </summary>
        /// <param name="timeoutHandle">cancellation handle returned by <see cref="SetTimeout"/>.
        /// No exception is thrown if handle is invalid or if callback execution has already been cancelled.</param>
        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is SetTimeoutCancellationHandle w)
            {
                w.Cts.Cancel();
            }
        }

        private class SetTimeoutCancellationHandle
        {
            public CancellationTokenSource Cts { get; set; }
        }
    }
}
