using Kabomu.Concurrency;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Concurrency
{
    public class VirtualTimeBasedEventLoopApiTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public VirtualTimeBasedEventLoopApiTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TestRunExclusively()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
            var expected = false;

            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            instance.RunExclusively(() =>
            {
                Assert.Equal(0, instance.CurrentTimestamp);
                tcs.SetResult(instance.IsInterimEventLoopThread);
            });

            var maxPendingEventCount = instance.PendingEventCount;

            // act.
            await instance.AdvanceTimeBy(0);

            // assert.
            var actual = await tcs.Task;
            Assert.Equal(0, instance.PendingEventCount);
            Assert.Equal(1, maxPendingEventCount);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestSetImmediate()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
            var expected = new List<string>();
            var actual = new List<string>();
            var tasks = new List<List<Task>>();

            for (int i = 0; i < 100; i++)
            {
                var capturedIndex = i;
                expected.Add("" + capturedIndex + false);
                var cancellationHandles = new List<object>();
                Func<Task<int>> cb = () =>
                {
                    Assert.Equal(0, instance.CurrentTimestamp);
                    cancellationHandles.ForEach(h => instance.ClearImmediate(h));
                    actual.Add("" + capturedIndex + instance.IsInterimEventLoopThread);
                    return Task.FromResult(capturedIndex);
                };

                var related = new List<Task>();
                var result = instance.SetImmediate(cb);
                related.Add(result.Item1);
                cancellationHandles.Add(result.Item2);

                // the rest should not execute.
                for (int j = 0; j < 2; j++)
                {
                    result = instance.SetImmediate(async () =>
                    {
                        await cb.Invoke();
                    });
                    related.Add(result.Item1);
                    cancellationHandles.Add(result.Item2);
                }
                tasks.Add(related);
            }

            // this must finish executing after all previous tasks have executed.
            var lastOne = instance.SetImmediate(() => Task.CompletedTask).Item1;

            var maxPendingEventCount = instance.PendingEventCount;

            // act
            await instance.AdvanceTimeBy(0);

            // assert
            await lastOne;
            Assert.Equal(0, instance.PendingEventCount);
            Assert.Equal(tasks.SelectMany(t => t).Count() + 1, maxPendingEventCount);

            // check cancellations
            foreach (var related in tasks)
            {
                for (var i = 0; i < related.Count; i++)
                {
                    var t = related[i];
                    if (i == 0)
                    {
                        await t;
                    }
                    else
                    {
                        Assert.False(t.IsCompleted);
                    }
                }
            }

            // finally ensure correct ordering of execution of tasks.
            new OutputEventLogger { Logs = actual }.AssertEqual(expected, _outputHelper);
        }

        [Fact]
        public async Task TestSetTimeout()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
            var expected = new List<string>();
            var actual = new List<string>();
            var tasks = new List<List<Task>>();

            for (int i = 0; i < 50; i++)
            {
                var capturedIndex = i;
                expected.Insert(0, "" + capturedIndex + false); // ie reverse
                var cancellationHandles = new List<object>();
                int timeoutValue = 4000 - 50 * i;
                Func<Task<int>> cb = () =>
                {
                    Assert.Equal(timeoutValue, instance.CurrentTimestamp);
                    cancellationHandles.ForEach(h => instance.ClearTimeout(h));
                    actual.Add("" + capturedIndex + instance.IsInterimEventLoopThread);
                    return Task.FromResult(capturedIndex);
                };

                var related = new List<Task>();
                var result = instance.SetTimeout(timeoutValue, cb);
                related.Add(result.Item1);
                cancellationHandles.Add(result.Item2);

                // the rest should not execute.
                for (int j = 0; j < 2; j++)
                {
                    result = instance.SetTimeout(timeoutValue, cb);
                    related.Add(result.Item1);
                    cancellationHandles.Add(result.Item2);
                }
                tasks.Add(related);
            }

            // this must finish executing after all previous tasks have executed.
            var lastOne = instance.SetTimeout(5000, () => Task.CompletedTask).Item1;

            var maxPendingEventCount = instance.PendingEventCount; 

            // act.
            await instance.AdvanceTimeTo(5000);

            // assert
            await lastOne;
            Assert.Equal(0, instance.PendingEventCount);
            Assert.Equal(tasks.SelectMany(t => t).Count() + 1, maxPendingEventCount);

            // check cancellations
            foreach (var related in tasks)
            {
                for (var i = 0; i < related.Count; i++)
                {
                    var t = related[i];
                    if (i == 0)
                    {
                        await t;
                    }
                    else
                    {
                        Assert.False(t.IsCompleted);
                    }
                }
            }

            // finally ensure correct ordering of execution of tasks.
            new OutputEventLogger { Logs = actual }.AssertEqual(expected, _outputHelper);
        }

        [Fact]
        public async Task TestForDeadlockAvoidance()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var laterTask = instance.SetTimeout(1800, () =>
            {
                tcs.SetResult(null);
                return Task.CompletedTask;
            }).Item1;
            var dependentTask = instance.SetTimeout(500, async () =>
            {
                await tcs.Task;
            }).Item1;

            // act
            await instance.AdvanceTimeTo(2_000);
            
            // assert completion.
            await dependentTask;
            await laterTask;
        }

        [Fact]
        public async Task TestAdvanceTimeIndefinitely()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var laterTask = instance.SetTimeout(1800, () =>
            {
                tcs.SetResult(null);
                return Task.CompletedTask;
            }).Item1;
            var dependentTask = instance.SetTimeout(500, async () =>
            {
                await tcs.Task;
            }).Item1;

            // act
            await instance.AdvanceTimeIndefinitely();

            // assert completion.
            await dependentTask;
            await laterTask;
        }
    }
}
