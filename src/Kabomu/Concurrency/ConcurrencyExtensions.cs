using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides helpful functions for concurrency control in the Kabomu library.
    /// </summary>
    public static class ConcurrencyExtensions
    {
        /// <summary>
        /// Enables the use of synchronous and asynchronous mutual exclusion schemes with the syntax:
        /// <code>
        /// using (await mutexApi.Synchronize())
        /// {
        ///     /* put protected code here */
        /// }
        /// </code>
        /// </summary>
        /// <param name="mutexApi">the mutual exclusion scheme. can be null.</param>
        /// <returns>an object which implements the awaiter protocol needed for mutual exclusion scheme to work
        /// like "lock" keyword, but through the use of "using" and "async/await" keywords.</returns>
        public static MutexAwaitable Synchronize(this IMutexApi mutexApi)
        {
            return new MutexAwaitable(mutexApi);
        }

        /// <summary>
        /// Schedules a callback for "immediate" execution on an event loop and makes it possible to
        /// wait for its completeion.
        /// </summary>
        /// <param name="eventLoopApi">the event loop instance</param>
        /// <param name="cb">the callback to execute soon</param>
        /// <returns>Pair of a task which can be used to wait for the execution to complete, and an object
        /// which can be passed to <see cref="IEventLoopApi.ClearImmediate"/> to cancel the execution and cause 
        /// the returned task to never complete.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="eventLoopApi"/> or
        /// <paramref name="cb"/> argument is null.</exception>
        public static (Task, object) WhenSetImmediate(this IEventLoopApi eventLoopApi, Func<Task> cb)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellationHandle = eventLoopApi.SetImmediate(() =>
            {
                _ = ProcessCallback(cb, tcs);
            });
            return (tcs.Task, cancellationHandle);
        }

        /// <summary>
        /// Schedules a callback to execute after a given time period and makes it possible to 
        /// wait for its completion.
        /// </summary>
        /// <param name="timerApi">the timer api instance</param>
        /// <param name="millis">wait time period before execution in milliseconds.</param>
        /// <param name="cb">the callback to execute after some time</param>
        /// <returns>Pair of a task which can be used to wait for the execution to complete, and an object
        /// which can be passed to <see cref="ITimerApi.ClearTimeout"/> to cancel the execution and cause 
        /// the returned task to never complete.</returns>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="timerApi"/> or
        /// <paramref name="cb"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="millis"/> argument is negative.</exception>
        public static (Task, object) WhenSetTimeout(this ITimerApi timerApi, Func<Task> cb, int millis)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var cancellationHandle = timerApi.SetTimeout(() =>
            {
                _ = ProcessCallback(cb, tcs);
            }, millis);
            return (tcs.Task, cancellationHandle);
        }

        private static async Task ProcessCallback(Func<Task> cb, TaskCompletionSource<object> tcs)
        {
            Task outcome;
            try
            {
                outcome = cb.Invoke();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }
            try
            {
                await outcome;
                tcs.SetResult(null);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }
    }
}
