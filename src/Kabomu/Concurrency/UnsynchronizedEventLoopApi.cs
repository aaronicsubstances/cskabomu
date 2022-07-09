using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides implementation of event loop that does not support the mutex api,
    /// does not support clearImmediate(), and runs setImmediate() and setTimeout() callbacks 
    /// without mutual exclusion.
    /// </summary>
    public class UnsynchronizedEventLoopApi : IEventLoopApi
    {
        public Tuple<Task, object> SetImmediate(Func<Task> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            var task = Task.Run(cb);
            return Tuple.Create<Task, object>(task, null);
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

        public void ClearImmediate(object immediateHandle)
        {
            throw new NotImplementedException();
        }

        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is CancellationTokenSource cts)
            {
                cts.Cancel();
            }
        }
    }
}
