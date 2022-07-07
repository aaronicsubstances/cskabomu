using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides default implementation of event loop that runs on the system thread pool.
    /// </summary>
    public class DefaultEventLoopApi : IEventLoopApi
    {
        [ThreadStatic]
        private static Thread _postCallbackExecutionThread;

        private readonly Action<object> UnwrapAndRunExclusivelyCallback;
        private readonly LimitedConcurrencyLevelTaskSchedulerInternal _throttledTaskScheduler;

        public DefaultEventLoopApi()
        {
            UnwrapAndRunExclusivelyCallback = UnwrapAndRunExclusively;
            _throttledTaskScheduler = new LimitedConcurrencyLevelTaskSchedulerInternal(1);
        }

        public bool IsInterimEventLoopThread => Thread.CurrentThread == _postCallbackExecutionThread;

        public void RunExclusively(Action cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
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

        public Task SetImmediate(CancellationToken cancellationToken, Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            Func<Task> cbWrapper = async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
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
            };
            PostCallback(Task.CompletedTask, cbWrapper, null);
            return tcs.Task;
        }

        public Task SetTimeout(int millis, CancellationToken cancellationToken, Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            return Task.Delay(millis, cancellationToken).ContinueWith(t =>
            {
                return SetImmediate(cancellationToken, cb);
            }).Unwrap();
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
                if (t.IsCompletedSuccessfully)
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
    }
}