using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides default implementation of timer API which runs callbacks without any mutual exclusion.
    /// </summary>
    public class DefaultTimerApi : ITimerApi
    {
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

        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is CancellationTokenSource cts)
            {
                cts.Cancel();
            }
        }
    }
}
