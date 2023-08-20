using Kabomu.Common;
using Kabomu.Tests.Shared.Common;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.IntegrationTests.Common
{
    public class SetIntervalWorkerTest
    {
        [Fact]
        public async Task TestNoWork()
        {
            // arrange
            var instance = new SetIntervalWorker();

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
        }

        [Fact]
        public async Task TestWorkOneOff()
        {
            // arrange
            var callCount = 0;
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = -1,
                DoWorkFunc = async () =>
                {
                    callCount++;
                    return false;
                }
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task TestInternalPending()
        {
            // arrange
            var callCount = 0;
            var instance = new SetIntervalWorker
            {
                ContinueCurrentExecutionOnWorkTimeout = true,
                DoWorkFunc = async () =>
                {
                    callCount++;
                    return callCount < 3;
                }
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.Equal(3, callCount);
        }

        [Fact]
        public async Task TestExternalPending()
        {
            // arrange
            var callCount = 0;
            var internalRes = false;
            var instance = new SetIntervalWorker();
            instance.DoWorkFunc = async () =>
            {
                callCount++;
                if (callCount < 10)
                {
                    // to set externalPending to true.
                    internalRes = await instance.TryStartWork();
                }
                return null;
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.False(internalRes);
            Assert.Equal(10, callCount);
        }

        [Fact]
        public async Task TestAsyncWorkForPossibleInterleaving()
        {
            // arrange
            var callCount = 0;
            var instance = new SetIntervalWorker
            {
                DoWorkFunc = async () =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(200);
                    return callCount < 10;
                }
            };

            async Task CreateTask1(int delayMs)
            {
                await Task.Delay(delayMs);
                await instance.TryStartWork();
            }

            var tasks = new List<Task>{ instance.TryStartWork() };
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(CreateTask1(1000 + (i * 150)));
            }

            async Task CreateTask2()
            {
                await Task.Delay(2200);
                instance.DoWorkFunc = async () =>
                {
                    Interlocked.Increment(ref callCount);
                    await Task.Delay(200);
                    return callCount < 20;
                };
                await instance.TryStartWork();
            }

            tasks.Add(CreateTask2());
            await ComparisonUtils.WhenAnyFailOrAllSucceed(tasks);

            // assert
            Assert.Equal(20, callCount);
        }

        [Fact]
        public async Task TestTimeoutWithReportCallback()
        {
            // arrange
            var testStartTime = DateTime.UtcNow;
            int callCount = 0, reportCallCount = 0;
            var reportTime = DateTime.MinValue;
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = 1,
                DoWorkFunc = async () =>
                {
                    callCount++;
                    await Task.Delay(2000);
                    return false;
                },
                ReportWorkTimeoutFunc = async t =>
                {
                    reportTime = t;
                    reportCallCount++;
                }
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.Equal(1, callCount);
            Assert.Equal(1, reportCallCount);
            Assert.InRange(reportTime, testStartTime.AddSeconds(-1),
                testStartTime.AddSeconds(1));
        }

        [Fact]
        public async Task TestTimeoutWithContinueCurrentExecutionOnWorkTimeout()
        {
            // arrange
            var testStartTime = DateTime.UtcNow;
            int callCount = 0, reportCallCount = 0;
            var reportTimes = new List<DateTime>();
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = 1,
                ContinueCurrentExecutionOnWorkTimeout = true,
                DoWorkFunc = async () =>
                {
                    callCount++;
                    await Task.Delay(2500);
                    return false;
                },
                ReportWorkTimeoutFunc = async t =>
                {
                    reportTimes.Add(t);
                    reportCallCount++;
                }
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.Equal(1, callCount);
            Assert.Equal(2, reportCallCount);
            Assert.InRange(reportTimes[0], testStartTime.AddSeconds(-1),
                testStartTime.AddSeconds(1));
            Assert.InRange(reportTimes[1], reportTimes[0].AddSeconds(-1),
                reportTimes[0].AddSeconds(1));
        }

        [Fact]
        public async Task TestAsyncWorkCompletionBeforeTimeout()
        {
            // arrange
            int callCount = 0, reportCallCount = 0;
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = 4,
                DoWorkFunc = async () =>
                {
                    callCount++;
                    await Task.Delay(2000);
                    return false;
                },
                ReportWorkTimeoutFunc = async _ =>
                {
                    reportCallCount++;
                }
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.Equal(1, callCount);
            Assert.Equal(0, reportCallCount);
        }

        [Fact]
        public async Task TestTimeoutWithNoReportCallback()
        {
            // arrange
            var callCount = 0;
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = 1,
                DoWorkFunc = async () =>
                {
                    callCount++;
                    await Task.Delay(2500);
                    return true;
                }
            };

            // act
            var actual = await instance.TryStartWork();

            // assert
            Assert.True(actual);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public async Task TestWithError1()
        {
            // arrange
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = -20,
                DoWorkFunc = () => Task.FromException<bool?>(
                    new Exception("error 1"))
            };

            // act
            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                instance.TryStartWork());

            // assert
            Assert.Equal("error 1", actualEx.Message);

            // confirm that doWorkFunc was the problem
            instance.DoWorkFunc = null;
            var retryResult = await instance.TryStartWork();
            Assert.True(retryResult);
        }

        [Fact]
        public async Task TestWithError2()
        {
            // arrange
            var instance = new SetIntervalWorker
            {
                WorkTimeoutSecs = 2000,
                ContinueCurrentExecutionOnWorkTimeout = true,
                DoWorkFunc = async () =>
                {
                    await Task.Delay(1200);
                    throw new Exception("error 2");
                }
            };

            // act
            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                instance.TryStartWork());

            // assert
            Assert.Equal("error 2", actualEx.Message);

            // confirm that doWorkFunc was the problem
            instance.DoWorkFunc = null;
            var retryResult = await instance.TryStartWork();
            Assert.True(retryResult);
        }
    }
}
