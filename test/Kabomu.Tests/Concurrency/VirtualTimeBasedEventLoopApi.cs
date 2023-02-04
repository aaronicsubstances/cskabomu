using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Tests.Concurrency
{
    /// <summary>
    /// Timer api implementation which doesn't use real time.
    /// Useful for testing software components that depend on real time.
    /// </summary>
    public class VirtualTimeBasedEventLoopApi : ITimerApi
    {
        private readonly object _mutex = new object();
        private readonly List<TaskDescriptor> _taskQueue = new List<TaskDescriptor>();
        private int _cancelledTaskCount = 0;
        private int _idSeq = 0;
        private long _currentTimestamp;
        private Func<Task> _stickyCallbackAftermathDelayance;
        private Func<Task> _defaultCallbackAftermathDelayance;

        /// <summary>
        /// Constructs a new instance with a current virtual timestamp of zero.
        /// </summary>
        public VirtualTimeBasedEventLoopApi()
        {
        }

        public Func<Task> StickyCallbackAftermathDelayance
        {
            set
            {
                lock (_mutex)
                {
                    _stickyCallbackAftermathDelayance = value;
                }
            }
        }

        public Func<Task> DefaultCallbackAftermathDelayance
        {
            set
            {
                lock (_mutex)
                {
                    _defaultCallbackAftermathDelayance = value;
                }
            }
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
        /// E.g. if one calls only AdvanceTimeBy(), then this value will increase monotonically.
        /// </remarks>
        public long CurrentTimestamp
        {
            get
            {
                lock (_mutex)
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
                lock (_mutex)
                {
                    return _taskQueue.Count - _cancelledTaskCount;
                }
            }
        }

        /// <summary>
        /// Advances time forward by a given value and executes all pending callbacks and any recursively scheduled callbacks
        /// whose scheduled time do not exceed the current virtual timestamp plus the given value.
        /// </summary>
        /// <remarks>
        /// Callers must ensure that at most only one Advance*() call processes callbacks a time, since multiple
        /// Advance*() will process callbacks concurrently, and the new virtual timestamp will be changed nondeterministically.
        /// <para></para>
        /// </remarks>
        /// <param name="delay">the value which when added to the current virtual timestamp will result in
        /// a new value for this instance, if this call completes without interference</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The <paramref name="delay"/> argument is negative.</exception>
        public Task AdvanceTimeBy(int delay)
        {
            if (delay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delay),
                    "cannot be negative. Received: " + delay);
            }
            long newTimestamp;
            lock (_mutex)
            {
                newTimestamp = _currentTimestamp + delay;
            }
            return AdvanceTimeTo(newTimestamp);
        }

        /// <summary>
        /// Advances time forward or backward to a given value and executes all pending callbacks and
        /// any recursively scheduled callbacks whose scheduled time do not exceed given value.
        /// </summary>
        /// <remarks> 
        /// Callers must ensure that at most only one Advance*() call processes callbacks a time, since multiple
        /// Advance*() will process callbacks concurrently, and the new virtual timestamp will be changed nondeterministically.
        /// <para></para>
        /// </remarks>
        /// <param name="newTimestamp">the new value of current virtual timestamp if this call completes
        /// without interference</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The <paramref name="newTimestamp"/> argument is negative.</exception>
        public Task AdvanceTimeTo(long newTimestamp)
        {
            if (newTimestamp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(newTimestamp),
                    "cannot be negative. Received: " + newTimestamp);
            }
            return TriggerActions(newTimestamp);
        }

        /// <summary>
        /// Work horse of real time simulation up to some virtual timestamp.
        /// </summary>
        /// <param name="stoppageTimestamp">The virtual timestamp at which to stop simulations.</param>
        /// <returns>a task representing the asynchronous operation</returns>
        private async Task TriggerActions(long stoppageTimestamp)
        {
            // invoke task queue actions starting with tail of queue
            // and stop if item's time is in the future.
            // use tail instead of head since removing at end of array-backed list
            // is faster that from front, because of the shifting required
            while (true)
            {
                TaskDescriptor earliestTaskDescriptor;
                lock (_mutex)
                {
                    if (_taskQueue.Count == 0)
                    {
                        _currentTimestamp = stoppageTimestamp;
                        break;
                    }
                    earliestTaskDescriptor = _taskQueue[_taskQueue.Count - 1];
                    if (!earliestTaskDescriptor.Cancelled && earliestTaskDescriptor.ScheduledAt > stoppageTimestamp)
                    {
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

                        // set up default delayance which will be used unless callback
                        // changes it.
                        _stickyCallbackAftermathDelayance = _defaultCallbackAftermathDelayance;
                    }
                }

                if (!earliestTaskDescriptor.Cancelled)
                {
                    // execute callback's synchronous phase
                    earliestTaskDescriptor.Callback.Invoke();

                    // give enough time to execute callback's asynchronous phase
                    // with effective delayance.
                    Task callbackAftermath = null;
                    lock (_mutex)
                    {
                        callbackAftermath = _stickyCallbackAftermathDelayance?.Invoke();
                    }
                    if (callbackAftermath != null)
                    {
                        await callbackAftermath;
                    }
                }
            }
        }

        /// <summary>
        /// Schedules callback to be run in this instance at a given virtual time if not cancelled.
        /// If there are already callbacks scheduled at that time, the callback will execute after them at that time.
        /// </summary>
        /// <remarks>
        /// The callback will only be executed as a result of an ongoing or future call to one of the Advance*() methods.
        /// </remarks>
        /// <param name="cb">the callback to run</param>
        /// <param name="millis">the virtual time delay after the current virtual time by which time the callback
        /// will be executed</param>
        /// <returns>handle which can be used to cancel timeout request</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="millis"/> argument is negative.</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public object SetTimeout(Action cb, int millis)
        {
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            if (millis < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(millis),
                    "cannot be negative. Received: " + millis);
            }
            lock (_mutex)
            {
                var nextId = _idSeq++;
                var taskDescriptor = new TaskDescriptor
                {
                    Id = nextId,
                    Callback = cb,
                    ScheduledAt = _currentTimestamp + millis
                };
                InsertIntoSortedTasks(taskDescriptor);
                return taskDescriptor;
            }
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

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetTimeout(Action, int)"/>.
        /// </summary>
        /// <param name="timeoutHandle">cancellation handle returned from <see cref="SetTimeout(Action, int)"/>
        /// No exception is thrown if handle is invalid or if callback execution has already been cancelled.</param>
        public void ClearTimeout(object timeoutHandle)
        {
            if (timeoutHandle is TaskDescriptor t)
            {
                lock (_mutex)
                {
                    CancelTask(t.Id, t.ScheduledAt);
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

        public Task Delay(int millis)
        {
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            SetTimeout(() => tcs.SetResult(null), millis);
            return tcs.Task;
        }

        private class TaskDescriptor
        {
            public int Id { get; set; }
            public Action Callback { get; set; }
            public long ScheduledAt { get; set; }
            public bool Cancelled { get; set; }
        }
    }
}
