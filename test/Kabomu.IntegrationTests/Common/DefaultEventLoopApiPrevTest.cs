using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.IntegrationTests.Common
{
    //[Collection("SequentialTests")]
    public class DefaultEventLoopApiPrevTest
    {
        [Fact]
        public async Task TestForThreadInteferenceErrors()
        {
            var instance = new DefaultEventLoopApi();
            const int cbCount = 90;
            var expected = new StringBuilder();
            for (int i = 0; i < cbCount; i++)
            {
                expected.Append((char)(' ' + i + 1));
            }
            var actual = new StringBuilder();
            for (int i = 0; i < cbCount; i++)
            {
                var captured = i;
                instance.PostCallback(s =>
                {
                    // test that cb state is returned correctly
                    Assert.Equal(captured, s);

                    if (captured % 10 == 0)
                    {
                        // by forcing current thread to sleep once in a while, 
                        // any thread interference will be detected by the production
                        // of a wrong final result of appending.
                        Thread.Sleep(10);
                    }
                    
                    actual.Append((char)(' ' + captured + 1));
                }, captured);
            }

            // wait for callbacks to be executed.
            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Fact]
        public async Task TestForMemoryConsistencyErrors()
        {
            var instance = new DefaultEventLoopApi();
            const int cbCount = 100;
            var expected = new StringBuilder();
            for (int i = 0; i < cbCount; i++)
            {
                expected.Append(1);
            }
            var actual = new StringBuilder();
            // use to test side effects.
            bool isPrevIdxAnOddNumber = true;
            for (int i = 0; i < cbCount; i++)
            {
                var captured = i;
                instance.PostCallback(s =>
                {
                    // test that cb state is returned correctly
                    Assert.Equal(captured, s);

                    if (captured % 10 == 0)
                    {
                        // by forcing current thread to sleep once in a while, 
                        // any memory inconsistency will be detected by the production
                        // of a wrong final result of appending.
                        Thread.Sleep(10);
                    }
                    var actualSideEffectTestVar = (captured - 1) % 2 != 0;
                    actual.Append(isPrevIdxAnOddNumber == actualSideEffectTestVar ?
                        1 : 0);
                    isPrevIdxAnOddNumber = !isPrevIdxAnOddNumber;
                }, captured);
            }

            // wait for callbacks to be executed.
            await Task.Delay(TimeSpan.FromSeconds(2));

            Assert.Equal(expected.ToString(), actual.ToString());
        }

        [Theory]
        [MemberData(nameof(CreateTestTimeoutData))]
        public async Task TestTimeout(int delaySecs, bool cancel)
        {
            // contract to test is that during a PostCallback, cancel works regardless of
            // how long we spend inside it.
            var instance = new DefaultEventLoopApi();
            var startTime = DateTime.Now;
            DateTime? stopTime = null;
            instance.PostCallback(_ =>
            {
                object timeoutId = instance.ScheduleTimeout(delaySecs * 1000, _i =>
                {
                    stopTime = DateTime.Now;
                }, null);

                // test that even sleeping past schedule timeout delay AND cancelling
                // will still achieve cancellation.
                Thread.Sleep(TimeSpan.FromSeconds(Math.Max(0, delaySecs + (delaySecs % 2 == 0 ? 1 : -1))));
                if (cancel)
                {
                    instance.CancelTimeout(timeoutId);
                }
            }, null);

            // wait for any pending callback to execute.
            await Task.Delay(TimeSpan.FromSeconds(5));

            if (cancel)
            {
                Assert.Null(stopTime);
            }
            else
            {
                Assert.NotNull(stopTime);
                var expectedStopTime = startTime.AddSeconds(delaySecs);
                // allow some secs tolerance in comparison using 
                // time differences observed in failed/flaky test results.
                Assert.Equal(expectedStopTime, stopTime.Value, TimeSpan.FromSeconds(1.5));
            }
        }

        public static List<object[]> CreateTestTimeoutData()
        {
            return new List<object[]>
            {
                new object[]{ 0, false },
                new object[]{ 0, true },
                new object[]{ 1, false },
                new object[]{ 1, true },
                new object[]{ 2, false },
                new object[]{ 2, true },
                new object[]{ 3, false },
                new object[]{ 3, true }
            };
        }

        [Fact]
        public void TestRunExclusively()
        {
            var instance = new DefaultEventLoopApi();
            int runCount = 0;
            var tasks = new Task[3];
            for (int i = 0; i < tasks.Length; i++)
            {
                var tcs = new TaskCompletionSource<int>();
                tasks[i] = tcs.Task;
                Task.Run(() =>
                {
                    instance.RunExclusively(s =>
                    {
                        Assert.Equal("ea", s);
                        runCount++;
                        instance.RunExclusively(s =>
                        {
                            Assert.Equal(4, s);
                            runCount++;
                            tcs.SetResult(0);
                        }, 4);
                    }, "ea");
                });
            }
            Task.WaitAll(tasks);
            Assert.Equal(tasks.Length * 2, runCount);
        }
    }
}
