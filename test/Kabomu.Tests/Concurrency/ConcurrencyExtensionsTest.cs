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
            // arrange.
            IMutexApi eventLoopBased = new DefaultEventLoopApi();

            // act.
            Thread t = Thread.CurrentThread, u, v;
            using (await eventLoopBased.Synchronize())
            {
                u = Thread.CurrentThread;
                using (await eventLoopBased.Synchronize())
                {
                    v = Thread.CurrentThread;
                }
            }

            // assert.
            Assert.NotEqual(t, u);
            Assert.Equal(u, v);
        }

        [Fact]
        public async Task TestLockBasedMutex()
        {
            // arrange.
            IMutexApi lockBased = new LockBasedMutexApi();

            // act.
            Thread t = Thread.CurrentThread, u, v;
            using (await lockBased.Synchronize())
            {
                u = Thread.CurrentThread;
                using (await lockBased.Synchronize())
                {
                    v = Thread.CurrentThread;
                }
            }

            // assert.
            Assert.Equal(t, u);
            Assert.Equal(u, v);
        }

        [Fact]
        public async Task TestLockBasedMutexConvenienceWithNullArgument()
        {
            // arrange.
            IMutexApi lockBased = new LockBasedMutexApi(null);

            // act.
            Thread t = Thread.CurrentThread, u, v;
            using (await lockBased.Synchronize())
            {
                u = Thread.CurrentThread;
                using (await lockBased.Synchronize())
                {
                    v = Thread.CurrentThread;
                }
            }

            // assert.
            Assert.Equal(t, u);
            Assert.Equal(u, v);
        }

        [Fact]
        public async Task TestNullMutexConvenience()
        {
            // arrange.
            IMutexApi nullBased = null;

            // act.
            Thread t = Thread.CurrentThread, u, v;
            using (await nullBased.Synchronize())
            {
                u = Thread.CurrentThread;
                using (await nullBased.Synchronize())
                {
                    v = Thread.CurrentThread;
                }
            }

            // assert.
            Assert.Equal(t, u);
            Assert.Equal(u, v);
        }
    }
}
