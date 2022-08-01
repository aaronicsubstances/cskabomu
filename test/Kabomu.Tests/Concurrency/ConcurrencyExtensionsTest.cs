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

        internal static async Task TestRealTimeBasedTimerCancellationNonInterference(ITimerApi timerApi)
        {
            var cbResults = 0;
            var timeoutId1 = timerApi.SetTimeout(() =>
            {
                cbResults += 100;
            }, 780);
            timerApi.SetTimeout(() =>
            {
                cbResults += 10;
            }, 99);
            timerApi.ClearTimeout(timeoutId1);

            // check for invalid calls.
            timerApi.ClearTimeout(new object());
            timerApi.ClearTimeout(null);
            timerApi.ClearTimeout(6);

            // check whether naive implementations which accept any native cancellation handle
            // was used.
            var cts = new CancellationTokenSource();
            timerApi.ClearTimeout(cts);

            await Task.Delay(1000);

            // assert
            Assert.Equal(10, cbResults);
            Assert.False(cts.IsCancellationRequested);
        }

        internal static async Task TestRealTimeBasedEventLoopCancellationNonInterference(IEventLoopApi eventLoop)
        {
            var cbResults = 0;
            var timeoutId1 = eventLoop.SetTimeout(() =>
            {
                cbResults += 100;
            }, 780);
            var timeoutId2 = eventLoop.SetTimeout(() =>
            {
                object immediateId1 = null;
                eventLoop.SetImmediate(() =>
                {
                    cbResults += 1000;
                    Assert.NotNull(immediateId1);
                    eventLoop.ClearTimeout(immediateId1); // check whether wrong call will work
                });
                immediateId1 = eventLoop.SetImmediate(() =>
                {
                    cbResults += 2000;
                    eventLoop.ClearImmediate(eventLoop.SetImmediate(() =>
                    {
                        cbResults += 10_000;
                    }));
                });
                cbResults += 10;
            }, 99);
            eventLoop.ClearTimeout(timeoutId1);
            eventLoop.ClearImmediate(timeoutId2); // check whether wrong call will work

            // check for invalid calls.
            eventLoop.ClearTimeout(new object());
            eventLoop.ClearTimeout(null);
            eventLoop.ClearTimeout(6);

            // check for invalid calls.
            eventLoop.ClearImmediate(new object());
            eventLoop.ClearImmediate(null);
            eventLoop.ClearImmediate(6);

            // check whether naive implementations which accept any native cancellation handle
            // was used.
            var cts1 = new CancellationTokenSource();
            eventLoop.ClearTimeout(cts1);
            var cts2 = new CancellationTokenSource();
            eventLoop.ClearImmediate(cts2);

            await Task.Delay(1000);

            // assert.
            Assert.Equal(3010, cbResults);
            Assert.False(cts1.IsCancellationRequested);
            Assert.False(cts2.IsCancellationRequested);
        }
    }
}
