using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Event loop implementation which doesn't use real time.
    /// Useful for testing software components that depend on real time.
    /// </summary>
    public class VirtualTimeBasedEventLoopApi : IEventLoopApi
    {
        private readonly object _lock = new object();
        private readonly List<TaskDescriptor> _taskQueue = new List<TaskDescriptor>();
        private int _cancelledTaskCount = 0;
        private int _idSeq = 0;
        private long _currentTimestamp;
        private bool _advSuspend;
        private int _maxCallbackAsyncContinuationTimeoutMillis;

        /// <summary>
        /// Constructs a new instance with a current virtual timestamp of zero.
        /// </summary>
        public VirtualTimeBasedEventLoopApi()
        {
        }

        /// <summary>
        /// Enables asynchronous continuations of a callback to finish before the execution of
        /// the next callback by an instance of this class during Advance*() calls.
        /// <para></para>
        /// If a callback has asynchronous continuations to run, then it can set this property to true
        /// before the end of its synchronous execution. It can then run its asynchronous continuations
        /// and then set this property to false in order to resume advancing of an instance of this class.
        /// </summary>
        /// <remarks>
        /// Note that in spite of suspension, if the value of the <see cref="MaxCallbackAsyncContinuationTimeoutMillis"/>
        /// property is positive, then advances will be resumed by that time indicated, regardless of whether
        /// suspensions have been resumed or not. Only when this value is zero (or negative), will the 
        /// resumption of an advance be solely dependent on a callback's setting of this property to false.
        /// </remarks>
        public bool AdvanceSuspended
        {
            get
            {
                lock (_lock)
                {
                    return _advSuspend;
                }
            }
            set
            {
                lock (_lock)
                {
                    _advSuspend = value;
                    // notify any ongoing advance to resume work.
                    Monitor.PulseAll(_lock);
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum amount of time that a callback's asynchronous continuations may execute.
        /// At construction time value is 0, which means that no such maximum time is imposed.
        /// </summary>
        /// <remarks>
        /// This property is intended to be a bail out for callbacks which suspended advances with the
        /// <see cref="AdvanceSuspended"/> property and never resumed them.
        /// </remarks>
        public int MaxCallbackAsyncContinuationTimeoutMillis
        {
            get
            {
                lock (_lock)
                {
                    return _maxCallbackAsyncContinuationTimeoutMillis;
                }
            }
            set
            {
                lock (_lock)
                {
                    _maxCallbackAsyncContinuationTimeoutMillis = value;
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
        /// Advances time forward by a given value and executes all pending callbacks and any recursively scheduled callbacks
        /// whose scheduled time do not exceed the current virtual timestamp plus the given value.
        /// </summary>
        /// <remarks>
        /// Callers must ensure that at most only one Advance*() call processes callbacks a time, since multiple
        /// Advance*() will process callbacks concurrently, and the new virtual timestamp will be changed nondeterministically.
        /// <para></para>
        /// Also asynchronous continuations of each callback may run in parallel with future callback executions,
        /// unless a callback chooses to suspend and resume ongoing advances with the
        /// <see cref="AdvanceSuspended"/> property, and/or applies time waits with the 
        /// <see cref="MaxCallbackAsyncContinuationTimeoutMillis"/> property.
        /// </remarks>
        /// <param name="delay">the value which when added to the current virtual timestamp will result in
        /// a new value for this instance, if this call completes without interference</param>
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
        /// Advances time forward or backward to a given value and executes all pending callbacks and
        /// any recursively scheduled callbacks whose scheduled time do not exceed given value.
        /// </summary>
        /// <remarks> 
        /// Callers must ensure that at most only one Advance*() call processes callbacks a time, since multiple
        /// Advance*() will process callbacks concurrently, and the new virtual timestamp will be changed nondeterministically.
        /// <para></para>
        /// Also asynchronous continuations of each callback may run in parallel with future callback executions,
        /// unless a callback chooses to suspend and resume ongoing advances with the
        /// <see cref="AdvanceSuspended"/> property, and/or applies time waits with the 
        /// <see cref="MaxCallbackAsyncContinuationTimeoutMillis"/> property.
        /// </remarks>
        /// <param name="newTimestamp">the new value of current virtual timestamp if this call completes
        /// without interference</param>
        /// <returns>a task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The <paramref name="newTimestamp"/> argument is negative.</exception>
        public Task AdvanceTimeTo(long newTimestamp)
        {
            if (newTimestamp < 0)
            {
                throw new ArgumentException("negative timestamp value: " + newTimestamp);
            }
            return Task.Factory.StartNew(() => TriggerActions(newTimestamp),
                CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// Work horse of real time simulation up to some virtual timestamp.
        /// </summary>
        /// <param name="stoppageTimestamp">The virtual timestamp at which to stop simulations.</param>
        private void TriggerActions(long stoppageTimestamp)
        {
            // invoke task queue actions starting with tail of queue
            // and stop if item's time is in the future.
            // use tail instead of head since removing at end of array-backed list
            // is faster that from front, because of the shifting required
            while (true)
            {
                TaskDescriptor earliestTaskDescriptor;
                lock (_lock)
                {
                    // clear any pending suspension of advances from previous callback exceptions or
                    // suspension timeouts.
                    _advSuspend = false;

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
                    }
                }

                if (earliestTaskDescriptor.Cancelled)
                {
                    continue;
                }
                else
                {
                    // execute callback's synchronous phase
                    earliestTaskDescriptor.Callback.Invoke();

                    // execute callback's asynchronous phase
                    var startTime = DateTime.Now;
                    lock (_lock)
                    {
                        var initialAdvValue = _advSuspend;
                        var initialAsyncCbTimeout = _maxCallbackAsyncContinuationTimeoutMillis;
                        if (initialAdvValue || initialAsyncCbTimeout > 0)
                        {
                            // this means some async continuations intended.
                            while (true)
                            {
                                if (initialAdvValue == true && !_advSuspend)
                                {
                                    break;
                                }
                                try
                                {
                                    if (initialAsyncCbTimeout > 0)
                                    {
                                        var timeRemaining = (int)(initialAsyncCbTimeout -
                                            (DateTime.Now - startTime).TotalMilliseconds);
                                        if (timeRemaining <= 0)
                                        {
                                            break;
                                        }
                                        Monitor.Wait(_lock, timeRemaining);
                                    }
                                    else
                                    {
                                        Monitor.Wait(_lock);
                                    }
                                }
                                catch (ThreadInterruptedException) { }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Runs a callback exclusively of any others. In this event loop implementation that is the same 
        /// as calling <see cref="SetImmediate"/>.
        /// </summary>
        /// <param name="cb">the callback to run</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public void RunExclusively(Action cb)
        {
            SetImmediate(cb);
        }

        /// <summary>
        /// Schedules callback to be run in this instance at the current virtual time, ie "now", unless it is cancelled.
        /// If there are already callbacks scheduled "now", the callback will execute after them "now".
        /// </summary>
        /// <remarks>
        /// The callback will only be executed as a result of an ongoing or future call to one of the Advance*() methods.
        /// <para></para>
        /// This method is equivalent to calling <see cref="SetTimeout(Action, int)"/> method with a timeout value of zero,
        /// although cancellation will still have to be done with ClearImmediate rather than ClearTimeout.
        /// </remarks>
        /// <param name="cb">callback to run</param>
        /// <returns>handle which can be used to cancel immediate execution request</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public object SetImmediate(Action cb)
        {
            return SetTimeout(cb, 0, true);
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
            return SetTimeout(cb, millis, false);
        }

        private object SetTimeout(Action cb, int millis, bool forImmediate)
        {
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            if (millis < 0)
            {
                throw new ArgumentException("negative timeout value: " + millis);
            }
            TaskDescriptor taskDescriptor;
            lock (_lock)
            {
                var nextId = _idSeq++;
                taskDescriptor = new TaskDescriptor
                {
                    Id = nextId,
                    Callback = cb,
                    ScheduledAt = _currentTimestamp + millis
                };
                InsertIntoSortedTasks(taskDescriptor);
            }
            if (forImmediate)
            {
                return new SetImmediateCancellationHandle
                {
                    Id = taskDescriptor.Id,
                    ScheduledAt = taskDescriptor.ScheduledAt
                };
            }
            else
            {
                return new SetTimeoutCancellationHandle
                {
                    Id = taskDescriptor.Id,
                    ScheduledAt = taskDescriptor.ScheduledAt
                };
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
        /// Used to cancel the execution of a callback scheduled with <see cref="SetImmediate(Action)"/>.
        /// </summary>
        /// <param name="immediateHandle">cancellation handle returned from <see cref="SetImmediate(Action)"/>. 
        /// No exception is thrown if handle is invalid or if callback execution has already been cancelled.</param>
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

        /// <summary>
        /// Used to cancel the execution of a callback scheduled with <see cref="SetTimeout(Action, int)"/>.
        /// </summary>
        /// <param name="timeoutHandle">cancellation handle returned from <see cref="SetTimeout(Action, int)"/>
        /// No exception is thrown if handle is invalid or if callback execution has already been cancelled.</param>
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
            public Action Callback { get; set; }
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
