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
        /// <returns>Pair of a task which can be used to wait for the execution to complete, and an object
        /// which can be passed to <see cref="ClearImmediate"/> to cancel the execution and cause 
        /// the returned task to never complete.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public Tuple<Task, object> SetImmediate(Func<Task> cb)
        {
            return SetImmediate(new CancellationTokenSource(), cb);
        }

        private Tuple<Task, object> SetImmediate(CancellationTokenSource cancellationHandle, Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Func<Task> cbWrapper = async () =>
            {
                if (cancellationHandle.IsCancellationRequested)
                {
                    return;
                }
                await ProcessCallback(cb, tcs);
            };
            PostCallback(Task.CompletedTask, cbWrapper, null);
            return Tuple.Create<Task, object>(tcs.Task, 
                new SetImmediateCancellationHandleWrapper(cancellationHandle));
        }

        private async Task ProcessCallback(Func<Task> cb, TaskCompletionSource<object> tcs)
        {
            Task outcome;
            try
            {
                _postCallbackExecutionThread = Thread.CurrentThread;
                outcome = cb.Invoke();
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }
            finally
            {
                _postCallbackExecutionThread = null;
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
            var task = Task.Delay(millis, cancellationHandle.Token).ContinueWith(t =>
            {
                return SetImmediate(cancellationHandle, cb).Item1;
            }).Unwrap();
            return Tuple.Create<Task, object>(task,
                new SetTimeoutCancellationHandleWrapper(cancellationHandle));
        }

        private Task PostCallback(Task antecedent, Func<Task> successCallback,
            Func<Exception, Task> failureCallback)
        {
            if (antecedent == null)
            {
                throw new ArgumentException("null antecedent task");
            }
            Func<Task, Task> continuation = t =>
            {
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    if (successCallback != null)
                    {
                        return successCallback.Invoke();
                    }
                    else
                    {
                        return Task.CompletedTask;
                    }
                }
                else
                {
                    if (failureCallback != null)
                    {
                        return failureCallback.Invoke(SimplifyAggregateException(t.Exception));
                    }
                    else
                    {
                        return CreateEquivalentFailureTask<object>(t.Exception);
                    }
                }
            };

            // Hide custom task scheduler to make continuation tasks work seamlessly with async/await syntax,
            // which uses the default task scheduler.
            return antecedent.ContinueWith(continuation, CancellationToken.None,
                TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler,
                _throttledTaskScheduler).Unwrap();
        }

        private static Exception SimplifyAggregateException(AggregateException ex)
        {
            var simplifiedException = ex.InnerExceptions.Count == 1 ?
                ex.InnerExceptions[0] : ex;
            return simplifiedException;
        }

        private static Task<T> CreateEquivalentFailureTask<T>(AggregateException ex)
        {
            if (ex.InnerExceptions.Count == 1)
            {
                return Task.FromException<T>(ex.InnerExceptions[0]);
            }
            else
            {
                var equivalentFailureTask = new TaskCompletionSource<T>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                equivalentFailureTask.SetException(ex.InnerExceptions);
                return equivalentFailureTask.Task;
            }
        }

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetImmediate(Func{Task})"/>.
        /// </summary>
        /// <param name="immediateHandle">cancellation handle. Should be the second item in the pair
        /// returned by <see cref="SetImmediate(Func{Task})"/>. No exception is thrown if handle is invalid or
        /// callback execution has already been cancelled.</param>
        public void ClearImmediate(object immediateHandle)
        {
            if (immediateHandle is SetImmediateCancellationHandleWrapper w)
            {
                w.Cts.Cancel();
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
            if (timeoutHandle is SetTimeoutCancellationHandleWrapper w)
            {
                w.Cts.Cancel();
            }
        }

        private struct SetImmediateCancellationHandleWrapper
        {
            public CancellationTokenSource Cts;

            public SetImmediateCancellationHandleWrapper(CancellationTokenSource cts)
            {
                Cts = cts;
            }
        }

        private struct SetTimeoutCancellationHandleWrapper
        {
            public CancellationTokenSource Cts;

            public SetTimeoutCancellationHandleWrapper(CancellationTokenSource cts)
            {
                Cts = cts;
            }
        }
    }
}