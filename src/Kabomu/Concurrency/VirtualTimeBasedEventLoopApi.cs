using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Event loop implementation which doesn't use real time.
    /// Useful for testing main components of library.
    /// </summary>
    public class VirtualTimeBasedEventLoopApi : ISynchronizedEventLoopApi
    {
        private readonly object _lock = new object();
        private readonly List<TaskDescriptor> _taskQueue = new List<TaskDescriptor>();
        private int _cancelledTaskCount = 0;
        private int _idSeq = 0;
        private long _currentTimestamp;

        // use to prevent interleaving of trigger actions by cancelling previous triggers
        private bool[] _triggerActionsCancellationHandle = new bool[1];

        public long CurrentTimestamp
        {
            get
            {
                lock (_lock)
                {
                    return _currentTimestamp;
                }
            }
        }

        public int PendingEventCount
        {
            get
            {
                lock (_lock)
                {
                    return _taskQueue.Count - _cancelledTaskCount;
                }
            }
        }

        public bool IsInterimEventLoopThread => false;

        public Task AdvanceTimeBy(int delay)
        {
            if (delay < 0)
            {
                throw new ArgumentException("negative timeout value: " + delay);
            }
            long newTimestamp;
            lock (_lock)
            {
                newTimestamp = _currentTimestamp + delay;
            }
            return AdvanceTimeTo(newTimestamp);
        }

        public Task AdvanceTimeTo(long newTimestamp)
        {
            if (newTimestamp < 0)
            {
                throw new ArgumentException("negative timestamp value: " + newTimestamp);
            }
            return TriggerActions(newTimestamp, new bool[1], true);
        }

        public async Task AdvanceTimeIndefinitely(int pollDelayMillis)
        {
            while (true)
            {
                // look for earliest non-cancelled task descriptor,
                // and that is found toward the end of the task queue.
                long stoppageTime;
                lock (this)
                {
                    int idx = -1;
                    for (int i = _taskQueue.Count - 1; i >= 0; i--)
                    {
                        if (!_taskQueue[i].Cancelled)
                        {
                            idx = i;
                            break;
                        }
                    }
                    if (idx == -1)
                    {
                        break;
                    }
                    stoppageTime = _taskQueue[idx].ScheduledAt;
                }
                // wait for some time in order to avoid being blocked by any callback waiting for
                // a later or future task queue member to complete.
                await await Task.WhenAny(TriggerActions(stoppageTime, new bool[1], false),
                    Task.Delay(pollDelayMillis));
            }
        }

        private async Task TriggerActions(long stoppageTimestamp, bool[] cancellationHandle, bool advancingNormally)
        {
            lock (_lock)
            {
                _triggerActionsCancellationHandle[0] = true;
                _triggerActionsCancellationHandle = cancellationHandle;
            }
            // invoke task queue actions starting with tail of queue
            // and stop if item's time is in the future.
            // use tail instead of head since removing at end of array-backed list
            // is faster that from front, because of the shifting required
            while (true)
            {
                TaskDescriptor earliestTaskDescriptor;
                lock (_lock)
                {
                    if (cancellationHandle[0])
                    {
                        break;
                    }
                    if (_taskQueue.Count == 0)
                    {
                        cancellationHandle[0] = true;
                        _currentTimestamp = stoppageTimestamp;
                        break;
                    }
                    earliestTaskDescriptor = _taskQueue[_taskQueue.Count - 1];
                    if (!earliestTaskDescriptor.Cancelled && earliestTaskDescriptor.ScheduledAt > stoppageTimestamp)
                    {
                        cancellationHandle[0] = true;
                        _currentTimestamp = stoppageTimestamp;
                        break;
                    }

                    _taskQueue.RemoveAt(_taskQueue.Count - 1);
                    if (earliestTaskDescriptor.Cancelled)
                    {
                        _cancelledTaskCount--;
                    }
                    else
                    {
                        _currentTimestamp = earliestTaskDescriptor.ScheduledAt;
                    }
                }

                if (earliestTaskDescriptor.Cancelled)
                {
                    continue;
                }
                else
                {
                    // invoke without waiting to prevent deadlock,
                    // unless we are advancing indefinitely.
                    var t = ProcessTaskDescriptor(earliestTaskDescriptor);
                    if (!advancingNormally)
                    {
                        await t;
                    }
                }
            }
        }

        private static async Task ProcessTaskDescriptor(TaskDescriptor taskDescriptor)
        {
            try
            {
                await taskDescriptor.Callback.Invoke();
                taskDescriptor.Tcs.SetResult(null);
            }
            catch (Exception e)
            {
                taskDescriptor.Tcs.SetException(e);
            }
        }

        public void RunExclusively(Action cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            SetImmediate(() =>
            {
                cb.Invoke();
                return Task.CompletedTask;
            });
        }

        public Tuple<Task, object> SetImmediate(Func<Task> cb)
        {
            return SetTimeout(0, cb, true);
        }

        public Tuple<Task, object> SetTimeout(int millis, Func<Task> cb)
        {
            return SetTimeout(millis, cb, false);
        }

        private Tuple<Task, object> SetTimeout(int millis, Func<Task> cb, bool forImmediate)
        {
            if (millis < 0)
            {
                throw new ArgumentException("negative timeout value: " + millis);
            }
            if (cb == null)
            {
                throw new ArgumentException("null cb");
            }
            var nextId = _idSeq++;
            var taskDescriptor = new TaskDescriptor
            {
                Id = nextId,
                Callback = cb,
                Tcs = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously),
                ScheduledAt = _currentTimestamp + millis
            };
            lock (_lock)
            {
                InsertIntoSortedTasks(taskDescriptor);
            }
            object cancellationHandle;
            if (forImmediate)
            {
                cancellationHandle = new SetImmediateCancellationHandle
                {
                    Id = taskDescriptor.Id,
                    ScheduledAt = taskDescriptor.ScheduledAt
                };
            }
            else
            {
                cancellationHandle = new SetTimeoutCancellationHandle
                {
                    Id = taskDescriptor.Id,
                    ScheduledAt = taskDescriptor.ScheduledAt
                };
            }
            return Tuple.Create<Task, object>(taskDescriptor.Tcs.Task, cancellationHandle);
        }

        private void InsertIntoSortedTasks(TaskDescriptor taskDescriptor)
        {
            // stable sort in reverse order since we will be retrieving from end of list.
            // for speed, leverage already sorted nature of queue, and use inner loop
            // of insertion sort.
            int insertIdx = 0;
            for (int i = _taskQueue.Count - 1; i >= 0; i--)
            {
                if (_taskQueue[i].ScheduledAt > taskDescriptor.ScheduledAt)
                {
                    insertIdx = i + 1;
                    break;
                }
            }
            _taskQueue.Insert(insertIdx, taskDescriptor);
        }

        public void ClearImmediate(object immediateHandle)
        {
            if (immediateHandle is SetImmediateCancellationHandle cancellationHandle)
            {
                lock (_lock)
                {
                    CancelTask(cancellationHandle.Id, cancellationHandle.ScheduledAt);
                }
            }
        }

        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is SetTimeoutCancellationHandle cancellationHandle)
            {
                lock (_lock)
                {
                    CancelTask(cancellationHandle.Id, cancellationHandle.ScheduledAt);
                }
            }
        }

        private void CancelTask(int targetId, long targetScheduledAt)
        {
            // leverage already sorted nature of queue and
            // use binary search to quickly locate desired id.

            int idxToSearchAround = _taskQueue.BinarySearch(new TaskDescriptor
            {
                ScheduledAt = targetScheduledAt
            }, Comparer<TaskDescriptor>.Create((x, y) => -1 * x.ScheduledAt.CompareTo(y.ScheduledAt)));
            if (idxToSearchAround < 0)
            {
                return;
            }

            // search forwards and backwards from binary search result for desired id.
            int indexToCancel = -1;
            // search forward
            for (int i = idxToSearchAround; i < _taskQueue.Count; i++)
            {
                var task = _taskQueue[i];
                if (task.ScheduledAt != targetScheduledAt)
                {
                    break;
                }
                if (task.Id == targetId)
                {
                    indexToCancel = i;
                    break;
                }
            }
            if (indexToCancel == -1)
            {
                // search backward
                for (int i = idxToSearchAround - 1; i >= 0; i--)
                {
                    var task = _taskQueue[i];
                    if (task.ScheduledAt != targetScheduledAt)
                    {
                        break;
                    }
                    if (task.Id == targetId)
                    {
                        indexToCancel = i;
                        break;
                    }
                }
            }
            if (indexToCancel != -1)
            {
                var task = _taskQueue[indexToCancel];
                if (!task.Cancelled)
                {
                    task.Cancelled = true;
                    _cancelledTaskCount++;
                }
            }
        }

        private class TaskDescriptor
        {
            public int Id { get; set; }
            public Func<Task> Callback { get; set; }
            public TaskCompletionSource<object> Tcs { get; set; }
            public long ScheduledAt { get; set; }
            public bool Cancelled { get; set; }
        }

        private class SetImmediateCancellationHandle
        {
            public int Id { get; set; }
            public long ScheduledAt { get; set; }
        }

        private class SetTimeoutCancellationHandle
        {
            public int Id { get; set; }
            public long ScheduledAt { get; set; }
        }
    }
}
