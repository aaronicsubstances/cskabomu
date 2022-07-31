using Kabomu.Concurrency;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Concurrency
{
    public class DefaultEventLoopApiTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public DefaultEventLoopApiTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TestRunExclusively()
        {
            // arrange.
            var instance = new DefaultEventLoopApi();
            var expected = true;

            // act.
            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            instance.RunExclusively(() =>
            {
                tcs.SetResult(instance.IsInterimEventLoopThread);
            });

            var actual = await tcs.Task;
            Assert.Equal(expected, actual);
        }

        [Fact]
        public Task TestSetImmediate()
        {
            // arrange.
            var instance = new DefaultEventLoopApi();
            var expected = new List<string>();
            var actual = new List<string>();
            var cancelledTasks = new List<Task>();

            // act
            for (int i = 0; i < 100; i++)
            {
                var capturedIndex = i;
                expected.Add("" + capturedIndex + true);
                var cancellationHandles = new List<object>();
                Func<Task<int>> cb = () =>
                {
                    cancellationHandles.ForEach(h => instance.ClearImmediate(h));
                    actual.Add("" + capturedIndex + instance.IsInterimEventLoopThread);
                    return Task.FromResult(capturedIndex);
                };
                instance.WhenSetImmediate(() =>
                {
                    var result = instance.WhenSetImmediate(cb);
                    cancellationHandles.Add(result.Item2);

                    // the rest should not execute.
                    result = instance.WhenSetImmediate(cb);
                    cancelledTasks.Add(result.Item1);
                    cancellationHandles.Add(result.Item2);
                    result = instance.WhenSetImmediate(async () =>
                    {
                        await cb.Invoke();
                    });
                    cancelledTasks.Add(result.Item1);
                    cancellationHandles.Add(result.Item2);

                    return Task.CompletedTask;
                });
            }

            // assert
            // this must run after all previous callbacks have run.
            // due to how the actual additions are done by a nested setImmediate call,
            // we have to wrap our assertions inside a nested setImmediate as well.
            return instance.WhenSetImmediate(() =>
            {
                return instance.WhenSetImmediate(() =>
                {
                    // check cancellations
                    foreach (var t in cancelledTasks)
                    {
                        Assert.False(t.IsCompleted);
                    }

                    // finally ensure correct ordering of execution of tasks.
                    new OutputEventLogger { Logs = actual }.AssertEqual(expected, _outputHelper);
                    return Task.CompletedTask;
                }).Item1;
            }).Item1;
        }

        [Fact]
        public async Task TestSetTimeout()
        {
            // arrange.
            var instance = new DefaultEventLoopApi();
            var expected = new List<string>();
            var actual = new List<string>();
            var tasks = new List<List<Task>>();

            // act
            for (int i = 0; i < 50; i++)
            {
                var capturedIndex = i;
                expected.Insert(0, "" + capturedIndex + true); // ie reverse.
                var cancellationHandles = new List<object>();
                Func<Task<int>> cb = () =>
                {
                    cancellationHandles.ForEach(h => instance.ClearTimeout(h));
                    actual.Add("" + capturedIndex + instance.IsInterimEventLoopThread);
                    return Task.FromResult(capturedIndex);
                };
                // Since it is not deterministic as to which call to setTimeout will execute first,
                // race multiple tasks with cancellation.
                // NB: depends on correct working of setImmediate
                // Also 50 ms is more than enough to distinguish callback firing times
                // on the common operating systems (15ms max on Windows, 10ms max on Linux).
                int timeoutValue = 2500 - 50 * i;
                instance.WhenSetImmediate(() =>
                {
                    var related = new List<Task>();
                    for (int i = 0; i < 3; i++)
                    {
                        var result = instance.WhenSetTimeout(cb, timeoutValue);
                        related.Add(result.Item1);
                        cancellationHandles.Add(result.Item2);
                    }
                    tasks.Add(related);
                    return Task.CompletedTask;
                });
            }

            // this should finish executing after all previous tasks have executed.
            var starTime = DateTime.Now;
            await instance.WhenSetTimeout(() => Task.CompletedTask, 3000).Item1;
            var overallTimeTakenMs = (DateTime.Now - starTime).TotalMilliseconds;

            // assert
            Assert.True(Math.Abs(overallTimeTakenMs - 3000) < 1500);

            // check cancellations
            foreach (var related in tasks)
            {
                var winner = await Task.WhenAny(related);
                foreach (var t in related)
                {
                    if (t == winner)
                    {
                        Assert.True(t.IsCompleted);
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
            var instance = new DefaultEventLoopApi();
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
