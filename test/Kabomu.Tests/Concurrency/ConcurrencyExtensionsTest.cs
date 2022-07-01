using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Concurrency
{
    public class ConcurrencyExtensionsTest
    {
        [Fact]
        public async Task TestEventLoopBasedMutex()
        {
            IEventLoopApi eventLoop = new DefaultEventLoopApi();
            object lockObj = null;
            Thread t = Thread.CurrentThread, u, v;
            using (await eventLoop.LockAsync(lockObj))
            {
                u = Thread.CurrentThread;
                using (await eventLoop.LockAsync(lockObj))
                {
                    v = Thread.CurrentThread;
                }
            }
            Assert.NotEqual(t, u);
            Assert.Equal(u, v);
        }

        [Fact]
        public async Task TestOrdinaryLockBasedMutex()
        {
            IEventLoopApi eventLoop = null;
            object lockObj = new object();
            Thread t = Thread.CurrentThread, u, v;
            using (await eventLoop.LockAsync(lockObj))
            {
                u = Thread.CurrentThread;
                using (await eventLoop.LockAsync(lockObj))
                {
                    v = Thread.CurrentThread;
                }
                Assert.Equal(t, u);
            }
            Assert.Equal(t, u);
            Assert.Equal(u, v);
        }

        [Fact]
        public async Task TestSameThreadMaintenanceForNoMutex()
        {
            IEventLoopApi eventLoop = null;
            object lockObj = null;
            Thread t = Thread.CurrentThread, u, v;
            using (await eventLoop.LockAsync(lockObj))
            {
                u = Thread.CurrentThread;
                using (await eventLoop.LockAsync(lockObj))
                {
                    v = Thread.CurrentThread;
                }
                Assert.Equal(t, u);
            }
            Assert.Equal(t, u);
            Assert.Equal(u, v);
        }
    }
}
