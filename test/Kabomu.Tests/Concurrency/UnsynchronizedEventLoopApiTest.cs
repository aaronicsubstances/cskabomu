using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Concurrency
{
    public class UnsynchronizedEventLoopApiTest
    {
        [Fact]
        public async Task TestSetImmediate()
        {
            // arrange.
            var instance = new UnsynchronizedEventLoopApi();
            var cbCalled = false;
            var result = instance.SetImmediate(() =>
            {
                cbCalled = true;
                return Task.CompletedTask;
            });

            // act.
            await result.Item1;

            // assert.
            Assert.Null(result.Item2);
            Assert.True(cbCalled);
        }

        [Fact]
        public async Task TestSetTimeout()
        {
            // arrange
            var instance = new UnsynchronizedEventLoopApi();
            var expected = new List<int> { 3, 2 };
            var actual = new List<int>();

            var timeout1Result = instance.SetTimeout(3200, () =>
            {
                actual.Add(1);
                return Task.CompletedTask;
            });
            var timeout2Result = instance.SetTimeout(3000, () =>
            {
                actual.Add(2);
                instance.ClearTimeout(timeout1Result.Item2);
                return Task.CompletedTask;
            });
            var timeout3Result = instance.SetTimeout(2000, () =>
            {
                actual.Add(3);
                return Task.CompletedTask;
            });

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
    }
}
