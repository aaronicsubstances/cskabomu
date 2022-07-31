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

            var timeout1Result = instance.WhenSetTimeout(() =>
            {
                actual.Add(1);
                return Task.CompletedTask;
            }, 3200);
            var timeout2Result = instance.WhenSetTimeout(() =>
            {
                actual.Add(2);
                instance.ClearTimeout(timeout1Result.Item2);
                return Task.CompletedTask;
            }, 3000);
            var timeout3Result = instance.WhenSetTimeout(() =>
            {
                actual.Add(3);
                return Task.CompletedTask;
            }, 2000);

            // act.
            var starTime = DateTime.Now;
            await timeout2Result.Item1;
            var overallTimeTakenMs = (DateTime.Now - starTime).TotalMilliseconds;

            // assert
            Assert.True(Math.Abs(overallTimeTakenMs - 3000) < 1500);

            // check cancellations
            Assert.False(timeout1Result.Item1.IsCompleted);

            // finally ensure correct ordering of execution of tasks.
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestForDeadlockAvoidance()
        {
            // arrange.
            var instance = new DefaultTimerApi();
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var laterTask = instance.WhenSetTimeout(() =>
            {
                tcs.SetResult(null);
                return Task.CompletedTask;
            }, 1800).Item1;
            var dependentTask = instance.WhenSetTimeout(async () =>
            {
                await tcs.Task;
            }, 500).Item1;

            // act and assert completion.
            await dependentTask;
            await laterTask;
        }
    }
}
