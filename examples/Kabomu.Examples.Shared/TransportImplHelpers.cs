using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public static class TransportImplHelpers
    {
        public static CancellablePromise CreateCancellableTimeoutTask(
            int timeoutMillis, string timeoutMsg)
        {
            if (timeoutMillis <= 0)
            {
                return null;
            }
            var timeoutId = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMillis, timeoutId.Token)
                .ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        var timeoutError = new QuasiHttpException(timeoutMsg,
                            QuasiHttpException.ReasonCodeTimeout);
                        throw timeoutError;
                    }
                });
            return new CancellablePromise
            {
                Task = timeoutTask,
                CancellationTokenSource = timeoutId
            };
        }
    }
}
