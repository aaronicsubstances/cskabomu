using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides default implementation of event loop api that runs on the system thread pool, and runs
    /// all callbacks under mutual exclusion.
    /// </summary>
    public class DefaultEventLoopApi : IEventLoopApi
    {
        [ThreadStatic]
        private static Thread _postCallbackExecutionThread;

        private readonly Action<object> UnwrapAndRunExclusivelyCallback;
        private readonly LimitedConcurrencyLevelTaskSchedulerInternal _throttledTaskScheduler;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DefaultEventLoopApi()
        {
            UnwrapAndRunExclusivelyCallback = UnwrapAndRunExclusively;
            _throttledTaskScheduler = new LimitedConcurrencyLevelTaskSchedulerInternal(1);
        }

        /// <summary>
        /// Return true if and only if current thread is the one which was picked for the latest execution of a 
        /// callback, AND the aftermath of the execution has remained synchronous until this property was caled.
        /// </summary>
        /// <remarks>
        /// This class uses the CLR thread pool, and so even if true is returned for current thread, false will be 
        /// returned after an async operation is started.
        /// </remarks>
        public bool IsInterimEventLoopThread => Thread.CurrentThread == _postCallbackExecutionThread;

        /// <summary>
        /// Runs a callback in a similar way to <see cref="SetImmediate(Func{Task})"/>.
        /// </summary>
        /// <param name="cb">callback to run under mutual exclusion</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public void RunExclusively(Action cb)
        {
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            // try to reduce garbage collection by not reusing SetImmediate.
            // also let TaskScheduler.UnobservedTaskException handle any uncaught task exceptions.
            Task.Factory.StartNew(UnwrapAndRunExclusivelyCallback, cb, CancellationToken.None,
                TaskCreationOptions.None, _throttledTaskScheduler);
        }

        private void UnwrapAndRunExclusively(object cbState)
        {
            var continuation = (Action)cbState;
            try
            {
                _postCallbackExecutionThread = Thread.CurrentThread;
                continuation.Invoke();
            }
            finally
            {
                _postCallbackExecutionThread = null;
            }
        }

        /// <summary>
        /// Schedules a callback to execute after those currently awaiting execution if not cancelled.
        /// </summary>
        /// <param name="cb">the callback to execute soon</param>
        /// <returns>a cancellation handle  which can be passed to <see cref="ClearImmediate"/> to cancel the execution and cause 
        /// the callback to never run.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public object SetImmediate(Action cb)
        {
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            var cancellationHandle = new CancellationTokenSource();
            SetImmediate(cancellationHandle, cb);
            return new SetImmediateCancellationHandle
            {
                Cts = cancellationHandle
            };
        }

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetImmediate(Action)"/>.
        /// </summary>
        /// <param name="immediateHandle">cancellation handle returned from <see cref="SetImmediate(Action)"/>. 
        /// No exception is thrown if handle is invalid or if callback execution has already been cancelled.</param>
        public void ClearImmediate(object immediateHandle)
        {
            if (immediateHandle is SetImmediateCancellationHandle w)
            {
                w.Cts.Cancel();
            }
        }

        /// <summary>
        /// Schedules a callback to execute after those scheduled to execute at a given time period if not cancelled.
        /// </summary>
        /// <param name="cb">the callback to execute</param>
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
            Task.Delay(millis, cancellationHandle.Token).ContinueWith(_ =>
            {
                SetImmediate(cancellationHandle, cb);
            });
            return new SetTimeoutCancellationHandle
            {
                Cts = cancellationHandle
            };
        }

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetTimeout"/>.
        /// </summary>
        /// <param name="timeoutHandle">cancellation handle returned from <see cref="SetTimeout"/>
        /// No exception is thrown if handle is invalid or if callback execution has already been cancelled.</param>
        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is SetTimeoutCancellationHandle w)
            {
                w.Cts.Cancel();
            }
        }

        private void SetImmediate(CancellationTokenSource cancellationHandle, Action cb)
        {
            Task.Factory.StartNew(() =>
            {
                if (cancellationHandle.IsCancellationRequested)
                {
                    return;
                }
                try
                {
                    _postCallbackExecutionThread = Thread.CurrentThread;
                    cb.Invoke();
                }
                finally
                {
                    _postCallbackExecutionThread = null;
                }
            }, cancellationHandle.Token, TaskCreationOptions.None, _throttledTaskScheduler);
        }

        private class SetImmediateCancellationHandle
        {
            public CancellationTokenSource Cts { get; set; }
        }

        private class SetTimeoutCancellationHandle
        {
            public CancellationTokenSource Cts { get; set; }
        }
    }
}