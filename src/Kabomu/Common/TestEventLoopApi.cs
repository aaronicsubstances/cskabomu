using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    /// <summary>
    /// Event loop implementation which doesn't use real time.
    /// Useful for testing main components of library.
    /// </summary>
    public class TestEventLoopApi : IEventLoopApi
    {
        private class TaskDescriptor
        {
            public int Id { get; set; }

            public Action<object> Callback { get; set; }

            public object CallbackState { get; set; }

            public long ScheduledAt { get; set; }

            public bool Cancelled { get; set; }
        }

        private class CancellationHandle
        {
            public int Id { get; set; }

            public long ScheduledAt { get; set; }
        }

        private readonly List<TaskDescriptor> _taskQueue = new List<TaskDescriptor>();
        private int _cancelledTaskCount = 0;
        private int _idSeq = 0;

        public void AdvanceTimeBy(long delay)
        {
            if (delay < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(delay));
            }
            AdvanceTimeTo(CurrentTimestamp + delay);
        }

        public void AdvanceTimeTo(long newTimestamp)
        {
            if (newTimestamp < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(newTimestamp));
            }
            TriggerActions(newTimestamp);
            CurrentTimestamp = newTimestamp;
        }

        public long CurrentTimestamp { get; private set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public bool IsEventDispatchThread { get; set; }

        public int PendingEventCount
        {
            get
            {
                return _taskQueue.Count - _cancelledTaskCount;
            }
        }

        private void TriggerActions(long stoppageTimestamp)
        {
            // invoke task queue actions starting with tail of queue
            // and stop if item's time is in the future.
            // use tail instead of head since removing at end of array-backed list
            // is faster that from front, because of the shifting required
            while (_taskQueue.Count > 0)
            {
                var lastTask = _taskQueue[_taskQueue.Count - 1];
                if (!lastTask.Cancelled && lastTask.ScheduledAt > stoppageTimestamp)
                {
                    break;
                }

                _taskQueue.RemoveAt(_taskQueue.Count - 1);

                if (lastTask.Cancelled)
                {
                    _cancelledTaskCount--;
                }
                else
                {
                    CurrentTimestamp = lastTask.ScheduledAt;
                    var cb = lastTask.Callback;
                    try
                    {
                        cb.Invoke(lastTask.CallbackState);
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
                }
            }
        }
        public void RunCallback(Action<object> cb, object cbState)
        {
            PostCallback(cb, cbState);
        }

        public void PostCallback(Action<object> cb, object cbState)
        {
            ScheduleTimeout(0, cb, cbState);
        }

        public object ScheduleTimeout(int millis, Action<object> cb, object cbState)
        {
            if (millis < 0)
            {
                throw new ArgumentException("cannot be negative", nameof(millis));
            }

            var taskDescriptor = new TaskDescriptor
            {
                Id = GenerateNextId(),
                Callback = cb,
                CallbackState = cbState,
                ScheduledAt = CurrentTimestamp + millis
            };
            InsertIntoSortedTasks(taskDescriptor);
            var cancellationHndle = new CancellationHandle
            {
                Id = taskDescriptor.Id,
                ScheduledAt = taskDescriptor.ScheduledAt
            };
            return cancellationHndle;
        }

        private int GenerateNextId()
        {
            return _idSeq++;
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

        public void CancelTimeout(object id)
        {
            var cancellationHandle = id as CancellationHandle;
            if (cancellationHandle == null)
            {
                return;
            }

            // leverage already sorted nature of queue and
            // use binary search to quickly locate desired id.

            int idxToSearchAround = _taskQueue.BinarySearch(new TaskDescriptor
            {
                ScheduledAt = cancellationHandle.ScheduledAt
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
                if (task.ScheduledAt != cancellationHandle.ScheduledAt)
                {
                    break;
                }
                if (task.Id == cancellationHandle.Id)
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
                    if (task.ScheduledAt != cancellationHandle.ScheduledAt)
                    {
                        break;
                    }
                    if (task.Id == cancellationHandle.Id)
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
    }
}
