using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    /// <summary>
    /// Provides implementation of IMutexApi based on dear old locks.
    /// </summary>
    public class LockBasedMutexApi : IMutexApi, IMutexContextFactory
    {
        private readonly object _lockObj;

        /// <summary>
        /// Creates a new instance with an internally generated lock.
        /// </summary>
        public LockBasedMutexApi() :
            this (new object())
        { }

        /// <summary>
        /// Creates a new instance with a specified lock.
        /// </summary>
        /// <param name="lockObj">the lock to use. can be null, in which case no mutual exclusion will be done.</param>
        public LockBasedMutexApi(object lockObj)
        {
            _lockObj = lockObj;
        }

        /// <summary>
        /// Always returns false to indicate synchronous mutual exclusion scheme with 
        /// mjtex context manager.
        /// </summary>
        public bool IsExclusiveRunRequired => false;

        /// <summary>
        /// Runs callback under lock. If a null lock was provided at construction time,
        /// then callback is invoked directly.
        /// </summary>
        /// <param name="cb">callback to run under mutual exclusion</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="cb"/> argument is null.</exception>
        public void RunExclusively(Action cb)
        {
            if (cb == null)
            {
                throw new ArgumentNullException(nameof(cb));
            }
            if (_lockObj == null)
            {
                cb.Invoke();
            }
            else
            {
                lock (_lockObj)
                {
                    cb.Invoke();
                }
            }
        }

        /// <summary>
        /// Creates an IDisposable instance suitable for use as a mutex context with 
        /// <see cref="MutexAwaitable"/>.
        /// </summary>
        /// <returns>mutex context for null if lock provided at construction time was null.</returns>
        public IDisposable CreateMutexContext()
        {
            if (_lockObj == null)
            {
                return null;
            }
            else
            {
                bool lockTaken = false;
                try
                {
                    Monitor.Enter(_lockObj, ref lockTaken);
                }
                catch (Exception)
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(_lockObj);
                    }
                    throw;
                }
                return new LockBasedMutexContextManger(_lockObj, lockTaken);
            }
        }

        private readonly struct LockBasedMutexContextManger : IDisposable
        {
            private readonly object _lockObj;
            private readonly bool _lockTaken;

            public LockBasedMutexContextManger(object lockObj, bool lockTaken)
            {
                _lockObj = lockObj;
                _lockTaken = lockTaken;
            }

            public void Dispose()
            {
                if (_lockTaken)
                {
                    Monitor.Exit(_lockObj);
                }
            }
        }
    }
}
