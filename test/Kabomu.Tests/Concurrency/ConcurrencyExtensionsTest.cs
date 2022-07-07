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
            Thread t = Thread.CurrentThread, u, v, w, x;
            string expectedError = "error";
            string actualError3 = null, actualError4 = null;
            using (await eventLoopBased.Synchronize())
            {
                u = Thread.CurrentThread;
                await ExtraneousProcessing1();
                using (await eventLoopBased.Synchronize())
                {
                    await ExtraneousProcessing2();
                    v = Thread.CurrentThread;
                    try
                    {
                        await ExtraneousProcessing3();
                    }
                    catch (Exception e)
                    {
                        actualError3 = e.Message;
                    }
                    w = Thread.CurrentThread;
                    try
                    {
                        await ExtraneousProcessing4();
                    }
                    catch (Exception e)
                    {
                        actualError4 = e.Message;
                    }
                    x = Thread.CurrentThread;
                }
            }

            // assert.
            Assert.Equal(expectedError, actualError3);
            Assert.Equal(expectedError, actualError4);
            Assert.NotEqual(t, u);
            Assert.Equal(u, v);
            Assert.Equal(u, w);
            Assert.Equal(u, x);
        }

        private Task ExtraneousProcessing1()
        {
            return Task.CompletedTask;
        }

        private async Task ExtraneousProcessing2()
        {
            await ExtraneousProcessing1();
        }

        private Task ExtraneousProcessing3()
        {
            throw new Exception("error");
        }

        private async Task ExtraneousProcessing4()
        {
            await ExtraneousProcessing3();
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
