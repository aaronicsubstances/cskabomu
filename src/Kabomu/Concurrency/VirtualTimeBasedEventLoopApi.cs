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
    public class VirtualTimeBasedEventLoopApi : IEventLoopApi
    {
        private readonly object _lock = new object();
        private readonly List<TaskDescriptor> _taskQueue = new List<TaskDescriptor>();
        private int _cancelledTaskCount = 0;
        private int _idSeq = 0;
        private long _currentTimestamp;

        // use to prevent interleaving of trigger actions by cancelling previous triggers
        private bool[] _triggerActionsCancellationHandle = new bool[1];

        /// <summary>
        /// Constructs a new instance with a current virtual timestamp of zero.
        /// </summary>
        public VirtualTimeBasedEventLoopApi()
        {
        }

        /// <summary>
        /// Gets the current virtual timestamp value. This value is not related to real time in any way.
        /// </summary>
        /// <remarks>
        /// Note that any time a callback is scheduled for
        /// execution, the value of this property will be set to the scheduled time of the callback
        /// prior to the execution of the callback. Thus this value does not increase monotonically
        /// unless clients always advance time forward.
        /// <para></para>
        /// E.g. if one never calls AdvanceTimeTo(), then this value will increase monotonically.
        /// </remarks>
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

        /// <summary>
        /// Returns the current number of callbacks awaiting execution.
        /// </summary>
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

        /// <summary>
        /// Returns false to indicate that there is no notion of event loop thread supported by this class.
        /// </summary>
        public bool IsInterimEventLoopThread => false;

        /// <summary>
        /// Advances time by a given value and executes all pending callbacks and any recursively scheduled callbacks
        /// whose scheduled time do not exceed the current virtual timestamp plus the given value.
        /// </summary>
        /// <remarks>
        /// Any previously ongoing Advance*() call is cancelled.
        /// Also only the first blocking phase of each callback is waited for by this event loop; any
        /// "asynchronous break" in execution continues later in parallel with other callback executions.
        /// </remarks>
        /// <param name="delay">the value with which to increment current virtual timestamp. Note that if
        /// another Advance*() call is made, this call will be cancelled and the timestamp change will likely
        /// not take effect.</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The <paramref name="delay"/> argument is negative.</exception>
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

        /// <summary>
        /// Advances time to a given value and executes all pending callbacks and any recursively scheduled callbacks
        /// whose scheduled time do not exceed given value.
        /// </summary>
        /// <remarks>
        /// Any previously ongoing Advance*() call is cancelled.
        /// Also only the first blocking phase of each callback is waited for by this event loop; any
        /// "asynchronous break" in execution continues later in parallel with the other callback executions.
        /// </remarks>
        /// <param name="newTimestamp">the new value of current virtual timestamp. Note that if
        /// another Advance*() call is made, this call will be cancelled and the timestamp change will likely
        /// not take effect.</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The <paramref name="newTimestamp"/> argument is negative.</exception>
        public Task AdvanceTimeTo(long newTimestamp)
        {
            if (newTimestamp < 0)
            {
                throw new ArgumentException("negative timestamp value: " + newTimestamp);
            }
            return TriggerActions(newTimestamp, new bool[1], true);
        }

        /// <summary>
        /// Advances time indefinitely until there are no more pending or recursively scheduled callbacks. 
        /// Assumes that no callback executes for more than 500 ms in real time.
        /// </summary>
        /// <remarks>
        /// Any previously ongoing Advance*() call is cancelled.
        /// However unlike in AdvanceBy() and AdvanceTo() methods, each callback is awaited fully before the
        /// next callback executes.
        /// </remarks>
        /// <returns>a task representing the asynchronous operation.</returns>
        public Task AdvanceTimeIndefinitely()
        {
            return AdvanceTimeIndefinitely(500);
        }

        /// <summary>
        /// Advances time indefinitely with a given maximum amount of real time of callback execution.
        /// </summary>
        /// <remarks>
        /// Any previously ongoing Advance*() call is cancelled.
        /// However unlike in AdvanceBy() and AdvanceTo() methods, each callback is awaited fully before the
        /// next callback executes.
        /// </remarks>
        /// <param name="maxExecutionTime">the maximum amount of real time any pending or recursively scheduled callback
        /// will spend executing. This determines how long to wait for a callback to finish executing,
        /// before concluding that that callback may be blocked by another callback in this loop. So shorter times are
        /// preferred.</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        public async Task AdvanceTimeIndefinitely(int maxExecutionTime)
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
                var triggerTask = TriggerActions(stoppageTime, new bool[1], false);
                var timeoutTask = Task.Delay(maxExecutionTime);
                var firstCompletedTask = await Task.WhenAny(triggerTask, timeoutTask);
                if (firstCompletedTask == triggerTask)
                {
                    var cancelled = await triggerTask;
                    if (cancelled)
                    {
                        break;
                    }
                }
                else
                {
                    await timeoutTask;
                }
            }
        }

        private async Task<bool> TriggerActions(long stoppageTimestamp, bool[] cancellationHandle, bool advancingNormally)
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
                        return false;
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
            return true;
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

        /// <summary>
        /// Runs a callback in a similar way to <see cref="SetImmediate(Func{Task})"/>.
        /// </summary>
        /// <param name="cb">the callback to run</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
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

        /// <summary>
        /// Schedules callback to be run in this instance at the current virtual time, ie "now", unless it is cancelled.
        /// If there are already callbacks scheduled "now", the callback will execute after them "now".
        /// The callback will only be executed as a result of an ongoing or future call to one of the Advance*() methods.
        /// </summary>
        /// <remarks>
        /// In this event loop implementation, this method is equivalent to calling
        /// <see cref="SetTimeout(int, Func{Task}, bool)"/> method with a timeout value of zero.
        /// </remarks>
        /// <param name="cb">callback to run</param>
        /// <returns>handles which can be used to either wait for immediate execution, or cancel immediate execution request</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public Tuple<Task, object> SetImmediate(Func<Task> cb)
        {
            return SetTimeout(0, cb, true);
        }

        /// <summary>
        /// Schedules callback to be run in this instance at a given virtual time if not cancelled.
        /// If there are already callbacks scheduled at that time, the callback will execute after them at that time.
        /// The callback will only be executed as a result of an ongoing or future call to one of the Advance*() methods.
        /// </summary>
        /// <param name="millis">the virtual time delay after the current virtual time by which time the callback
        /// will be executed</param>
        /// <param name="cb">the callback to run</param>
        /// <returns>handles which can be used to either wait for timeout request, or cancel timeout request</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="millis"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
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
