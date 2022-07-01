using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Concurrency
{
    public static class ConcurrencyExtensions
    {
        public static LockAsyncAwaitable LockAsync(this IEventLoopApi eventLoop, object fallbackLockObj)
        {
            return new LockAsyncAwaitable(eventLoop, fallbackLockObj);
        }
    }
}
