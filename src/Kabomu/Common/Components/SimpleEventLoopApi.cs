using Kabomu.Common.Abstractions;
using Kabomu.Common.Helpers;
using Kabomu.Common.Internals;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common.Components
{
    /// <summary>
    /// Provides simple implementation of event loop that uses the system thread pool.
    /// </summary>
    public class SimpleEventLoopApi : IEventLoopApi
    {
        private readonly LimitedConcurrencyLevelTaskScheduler _throttledTaskScheduler;

        public SimpleEventLoopApi()
        {
            _throttledTaskScheduler = new LimitedConcurrencyLevelTaskScheduler(1);
        }

        public bool IsEventDispatchThread => false;

        public long CurrentTimestamp => DateTimeUtils.UnixTimeMillis;

        public void PostCallback(Action<object> cb, object cbState)
        {
            PostCallback(cb, cbState, CancellationToken.None);
        }

        private void PostCallback(Action<object> cb, object cbState, CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() => {
                try
                {
                    cb.Invoke(cbState);
                }
                catch (Exception ex)
                {
                    if (ErrorHandler == null)
                    {
                        throw ex;
                    }
                    else
                    {
                        ErrorHandler.Invoke(ex, "Error encountered in callback execution");
                    }
                }
            }, cancellationToken, TaskCreationOptions.None, _throttledTaskScheduler);
        }

        public object ScheduleTimeout(int millis, Action<object> cb, object cbState)
        {
            var cts = new CancellationTokenSource();
            Task.Delay(millis, cts.Token).ContinueWith(t =>
            {
                PostCallback(cb, cbState, cts.Token);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
            return cts;
        }

        public void CancelTimeout(object id)
        {
            if (id is CancellationTokenSource source)
            {
                source.Cancel();
            }
        }

        public UncaughtErrorCallback ErrorHandler { get; set; }
    }
}
