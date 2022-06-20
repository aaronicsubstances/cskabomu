using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common.Concurrency
{
    /// <summary>
    /// Provides default implementation of event loop that runs on the system thread pool.
    /// </summary>
    public class DefaultEventLoopApi : IEventLoopApi
    {
        private readonly LimitedConcurrencyLevelTaskScheduler _throttledTaskScheduler;

        public DefaultEventLoopApi()
        {
            _throttledTaskScheduler = new LimitedConcurrencyLevelTaskScheduler(1);
        }

        public Task SetImmediate(CancellationToken cancellationToken, Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            return PostCallback(Task.CompletedTask, cb, null, cancellationToken,
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task<T> SetImmediate<T>(CancellationToken cancellationToken, Func<Task<T>> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            return PostCallback(Task.CompletedTask, cb, null, cancellationToken, 
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task SetTimeout(int millis, CancellationToken cancellationToken, Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            return Task.Delay(millis, cancellationToken).ContinueWith(t =>
            {
                return PostCallback(t, cb, null, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion);
            }, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
        }

        public Task<T> SetTimeout<T>(int millis, CancellationToken cancellationToken, Func<Task<T>> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            return Task.Delay(millis, cancellationToken).ContinueWith(t =>
            {
                return PostCallback(t, cb, null, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion);
            }, TaskContinuationOptions.OnlyOnRanToCompletion).Unwrap();
        }

        private Task<T> PostCallback<T>(Task antecedent, Func<Task<T>> successCallback,
            Func<Exception, Task<T>> failureCallback, CancellationToken cancellationToken,
            TaskContinuationOptions taskContinuationOptions)
        {
            if (antecedent == null)
            {
                throw new ArgumentException("null antecedent task");
            }
            Func<Task, Task<T>> continuation = t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    if (successCallback != null)
                    {
                        return successCallback.Invoke();
                    }
                    else
                    {
                        return Task.FromResult(default(T));
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
                        return CreateEquivalentFailureTask<T>(t.Exception);
                    }
                }
            };

            // Hide custom task scheduler to make continuation tasks work seamlessly with async/await syntax,
            // which uses the default task scheduler.
            return antecedent.ContinueWith(continuation, cancellationToken,
                taskContinuationOptions | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler,
                _throttledTaskScheduler).Unwrap();
        }

        private Task PostCallback(Task antecedent, Func<Task> successCallback,
            Func<Exception, Task> failureCallback, CancellationToken cancellationToken, 
            TaskContinuationOptions taskContinuationOptions)
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
            return antecedent.ContinueWith(continuation, cancellationToken,
                taskContinuationOptions | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler,
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