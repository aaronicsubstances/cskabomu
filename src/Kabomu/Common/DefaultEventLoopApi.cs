using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Provides default implementation of event loop that uses the system thread pool.
    /// </summary>
    public class DefaultEventLoopApi : IEventLoopApi
    {
        private readonly LimitedConcurrencyLevelTaskSchedulerInternal _throttledTaskScheduler;

        public DefaultEventLoopApi()
        {
            _throttledTaskScheduler = new LimitedConcurrencyLevelTaskSchedulerInternal(1);
        }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public Task SetImmediateAsync()
    {
            return Task.CompletedTask.ContinueWith(t => { }, CancellationToken.None, TaskContinuationOptions.DenyChildAttach,
                _throttledTaskScheduler);
        }

        public Task SetImmediateAsync(CancellationToken cancellationToken)
        {
            // As long as cancellation is triggered from within event loop, swallow any TaskCanceledExceptions.
            // Else let cancellations triggered during run of continuation task lead to TaskCanceledExceptions.
            return Task.Factory.StartNew(() => { }, cancellationToken, TaskCreationOptions.DenyChildAttach,
                    _throttledTaskScheduler)
                .ContinueWith(t => t, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion,
                    _throttledTaskScheduler).Unwrap();
        }

        public Task SetTimeoutAsync(int millis)
        {
            return Task.Delay(millis).ContinueWith(t => { }, CancellationToken.None, TaskContinuationOptions.DenyChildAttach,
                _throttledTaskScheduler);
        }

        public Task SetTimeoutAsync(int millis, CancellationToken cancellationToken)
        {
            return Task.Delay(millis, cancellationToken)
                .ContinueWith(t => SetImmediateAsync(cancellationToken),
                    TaskContinuationOptions.OnlyOnRanToCompletion)
                .Unwrap();
        }

        public bool IsMutexRequired(out Task t)
        {
            if (LimitedConcurrencyLevelTaskSchedulerInternal.CurrentThreadIsProcessingItems)
            {
                t = null;
                return false;
            }
            t = SetImmediateAsync(CancellationToken.None);
            return true;
        }

        public Task MutexWrap(Task taskToWrap)
        {
            return taskToWrap.ContinueWith(t => t, CancellationToken.None, TaskContinuationOptions.DenyChildAttach,
                _throttledTaskScheduler).Unwrap();
        }

        public Task<T> MutexWrap<T>(Task<T> taskToWrap)
        {
            return taskToWrap.ContinueWith(t => t, CancellationToken.None, TaskContinuationOptions.DenyChildAttach,
                _throttledTaskScheduler).Unwrap();
        }
    }
}
