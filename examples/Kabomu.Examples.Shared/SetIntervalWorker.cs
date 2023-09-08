using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    /// <summary>
    /// Generic solution for wrapping function argument to equivalent of NodeJS setInterval,
    /// in order to avoid interleaving of function executions; and also
    /// to make it possible to invoke same function argument outside of
    /// setInterval mechanism.
    /// </summary>
    public class SetIntervalWorker
    {
        private readonly object _lock = new object();
        private bool _externalProceed;
        private bool _isCurrentlyExecuting;

        public virtual int WorkTimeoutSecs { get; set; }
        public virtual bool ContinueCurrentExecutionOnWorkTimeout { get; set; }
        public virtual Func<DateTime, Task> ReportWorkTimeoutFunc { get; set; }
        public virtual Func<Task<bool?>> DoWorkFunc { get; set; }

        public async Task<bool> TryStartWork()
        {
            lock (_lock)
            {
                _externalProceed = true;
                if (_isCurrentlyExecuting)
                {
                    return false;
                }
                _isCurrentlyExecuting = true;
            }
            await StartWork();
            return true;
        }

        private async Task StartWork()
        {
            bool continueLoop = true;
            while (continueLoop)
            {
                lock (_lock)
                {
                    _externalProceed = false; // also prevents endless looping
                                              // if no error is thrown.
                }

                var result = (false, false);
                var errOccured = false;
                try
                {
                    result = await DoWorkWIthTimeout();
                }
                catch (Exception)
                {
                    errOccured = true;
                    throw;
                }
                finally
                {
                    continueLoop = LoopPosUpdates(errOccured || result.Item1,
                        result.Item2);
                }
            }
        }

        private bool LoopPosUpdates(bool errOccured, bool internalProceed)
        {
            lock (_lock)
            {
                if (errOccured || !_externalProceed && !internalProceed)
                {
                    _isCurrentlyExecuting = false;
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        private async Task<(bool, bool)> DoWorkWIthTimeout()
        {
            var workTimeoutSecs = WorkTimeoutSecs;
            if (workTimeoutSecs == 0)
            {
                workTimeoutSecs = 3600; // default of 1 hour.
            }
            var pendingWorkTimestamp = DateTime.UtcNow;
            var actualWork = DoWorkFunc?.Invoke() ??
                Task.FromResult((bool?)null);
            // interpret negative value to mean disabling of timeout.
            if (workTimeoutSecs < 0)
            {
                var internalProceed = await actualWork;
                return (false, internalProceed ?? false);
            }
            do
            {
                var cts = new CancellationTokenSource();
                try
                {
                    var delayTask = Task.Delay(
                        TimeSpan.FromSeconds(workTimeoutSecs), cts.Token);
                    var winner = await Task.WhenAny(delayTask, actualWork);
                    if (winner == actualWork)
                    {
                        var internalProceed = await actualWork;
                        return (false, internalProceed ?? false);
                    }
                }
                finally
                {
                    cts.Cancel();
                }
                try
                {
                    // don't wait.
                    _ = ReportWorkTimeoutFunc?.Invoke(pendingWorkTimestamp);
                }
                catch { } // ignore
            } while (ContinueCurrentExecutionOnWorkTimeout);
            return (true, false);
        }
    }
}
