using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Concurrency
{
    public class DefaultTimerApiTest
    {
        [Fact]
        public async Task TestSetTimeout()
        {
            // arrange
            var instance = new DefaultTimerApi();
            var expected = new List<int> { 3, 2 };
            var actual = new List<int>();

            instance.SetTimeout(() =>
            {
                actual.Add(2);
            }, 1200);

            instance.SetTimeout(() =>
            {
                actual.Add(3);
            }, 700);

            // act.
            await instance.Delay(2000);

            // assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestClearTimeout()
        {
            var instance = new DefaultTimerApi();
            var callbackLogs = new List<string>();
            var timeoutId = instance.SetTimeout(() => callbackLogs.Add("sh"), 500);
            await instance.Delay(1000);
            instance.ClearTimeout(timeoutId);
            var expected = new List<string>
            {
                "sh"
            };
            Assert.Equal(expected, callbackLogs);

            callbackLogs.Clear();
            timeoutId = instance.SetTimeout(() => callbackLogs.Add("sh"), 500);
            await instance.Delay(110);
            instance.ClearTimeout(timeoutId);
            expected = new List<string>();
            Assert.Equal(expected, callbackLogs);

            // check that calls with invalid or used arguments cause no problems.
            callbackLogs.Clear();
            instance.ClearTimeout(timeoutId);
            instance.ClearTimeout(null);
            instance.ClearTimeout(5);
            instance.ClearTimeout("jfal");
            expected = new List<string>();
            Assert.Equal(expected, callbackLogs);
        }

        [Fact]
        public void TestForErrors()
        {
            var instance = new DefaultTimerApi();
            Assert.Throws<ArgumentNullException>(() =>
                instance.SetTimeout(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.SetTimeout(() => { }, -1));
        }

        [Fact]
        public async Task TestDelayWithDeadlockAvoidance()
        {
            // arrange.
            var instance = new DefaultTimerApi();
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            async Task RunLaterTask()
            {
                await instance.Delay(1800);
                tcs.SetResult(null);
            }

            async Task RunDependentTask()
            {
                await instance.Delay(500);
                await tcs.Task;
            }
            var laterTask = RunLaterTask();
            var dependentTask = RunDependentTask();

            // act
            await instance.Delay(2_000);

            // assert completion.
            await dependentTask;
            await laterTask;
        }
    }
}
