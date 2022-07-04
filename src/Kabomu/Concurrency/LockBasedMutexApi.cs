using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public class LockBasedMutexApi : IMutexApi
    {
        private readonly object _lockObj;

        public LockBasedMutexApi(object lockObj)
        {
            _lockObj = lockObj;
        }

        public bool IsExclusiveRunRequired => false;

        public void RunExclusively(Action cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null cb");
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

        public IDisposable CreateMutexContextManager()
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
