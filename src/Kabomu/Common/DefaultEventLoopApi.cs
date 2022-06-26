using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides default implementation of event loop that runs on the system thread pool.
    /// </summary>
    public class DefaultEventLoopApi : IEventLoopApi
    {
        private readonly LimitedConcurrencyLevelTaskSchedulerInternal _throttledTaskScheduler;

        public DefaultEventLoopApi()
        {
            _throttledTaskScheduler = new LimitedConcurrencyLevelTaskSchedulerInternal(1);
        }

        public Task SetImmediate(CancellationToken cancellationToken, Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            var tcs = new TaskCompletionSource<object>();
            if (cancellationToken.IsCancellationRequested)
            {
                return tcs.Task;
            }
            Func<Task> cbWrapper = async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                try
                {
                    await cb.Invoke();
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

        public Task<T> SetImmediate<T>(CancellationToken cancellationToken, Func<Task<T>> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            var tcs = new TaskCompletionSource<T>();
            if (cancellationToken.IsCancellationRequested)
            {
                return tcs.Task;
            }
            Func<Task> cbWrapper = async () =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                try
                {
                    T res = await cb.Invoke();
                    tcs.SetResult(res);
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

        public Task<T> SetTimeout<T>(int millis, CancellationToken cancellationToken, Func<Task<T>> cb)
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