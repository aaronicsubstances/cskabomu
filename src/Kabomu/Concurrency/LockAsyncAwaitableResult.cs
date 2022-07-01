using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Kabomu.Concurrency
{
    public struct LockAsyncAwaitable
    {
        private readonly IEventLoopApi _eventLoop;
        private readonly object _lockObj;

        public LockAsyncAwaitable(IEventLoopApi eventLoop, object lockObj)
        {
            _eventLoop = eventLoop;
            _lockObj = lockObj;
        }

        public LockAsyncAwaiter GetAwaiter()
        {
            return new LockAsyncAwaiter(_eventLoop, _lockObj);
        }
    }

    public struct LockAsyncAwaiter : INotifyCompletion
    {
        private readonly IEventLoopApi _eventLoop;
        private readonly object _lockObj;

        public LockAsyncAwaiter(IEventLoopApi eventLoop, object lockObj)
        {
            _eventLoop = eventLoop;
            _lockObj = lockObj;
        }

        public bool IsCompleted => _eventLoop == null || _eventLoop.IsInterimEventLoopThread;

        public void OnCompleted(Action continuation)
        {
            _eventLoop.SetImmediate(CancellationToken.None, async () => continuation.Invoke());
        }

        public IDisposable GetResult()
        {
            if (_lockObj != null)
            {
                return new LockBasedDisposableMutex(_lockObj);
            }
            else
            {
                return null;
            }
        }
    }

    struct LockBasedDisposableMutex : IDisposable
    {
        private readonly object _lockObj;
        private readonly bool _lockTaken;

        public LockBasedDisposableMutex(object lockObj)
        {
            bool lockTaken = false;
            try
            {
                Monitor.Enter(lockObj, ref lockTaken);
            }
            catch (Exception)
            {
                if (lockTaken) Monitor.Exit(lockObj);
                throw;
            }
            _lockObj = lockObj;
            _lockTaken = lockTaken;
        }

        public void Dispose()
        {
            if (_lockTaken) Monitor.Exit(_lockObj);
        }
    }
}
