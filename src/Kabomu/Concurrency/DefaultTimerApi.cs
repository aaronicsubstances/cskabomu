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
        /// Schedules a callback to execute after a given time period if not cancelled.
        /// </summary>
        /// <param name="millis">wait time period before execution in milliseconds.</param>
        /// <param name="cb">the callback to execute after some time</param>
        /// <returns>Pair of a task which can be used to wait for the execution to complete, and an object
        /// which can be passed to <see cref="ClearTimeout"/> to cancel the execution and cause 
        /// the returned task to never complete.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="millis"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public Tuple<Task, object> SetTimeout(int millis, Func<Task> cb)
        {
            if (millis < 0)
            {
                throw new ArgumentException("negative timeout value: " + millis);
            }
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            var cancellationHandle = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Func<Task, Task> cbWrapper = async t =>
            {
                if (t.IsCanceled)
                {
                    return;
                }
                await ProcessCallback(cb, tcs);
            };
            Task.Delay(millis, cancellationHandle.Token).ContinueWith(cbWrapper);
            return Tuple.Create<Task, object>(tcs.Task, cancellationHandle);
        }

        private async Task ProcessCallback(Func<Task> cb, TaskCompletionSource<object> tcs)
        {
            try
            {
                await cb.Invoke();
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetTimeout"/>.
        /// </summary>
        /// <param name="timeoutHandle">cancellation handle. Should be the second item in the pair
        /// returned by <see cref="SetTimeout"/>. No exception is thrown if handle is invalid or
        /// callback execution has already been cancelled.</param>
        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is CancellationTokenSource cts)
            {
                cts.Cancel();
            }
        }
    }
}
