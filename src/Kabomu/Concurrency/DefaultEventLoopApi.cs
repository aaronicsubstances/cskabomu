using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides implementation of event loop that runs on the system thread pool, and runs
    /// all callbacks under mutual exclusion.
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

        public Tuple<Task, object> SetTimeout(int millis, Func<Task> cb)
        {
            if (millis < 0)
            {
                throw new ArgumentException("negative timeout value: " + millis);
            }
            if (cb == null)
            {
                throw new ArgumentException("null cb");
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

        public void ClearImmediate(object immediateHandle)
        {
            if (immediateHandle is SetImmediateCancellationHandleWrapper w)
            {
                w.Cts.Cancel();
            }
        }

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