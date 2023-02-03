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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task TestAdvance(bool callAdvanceBy)
        {
            var instance = new VirtualTimeBasedEventLoopApi();
            Assert.Equal(0, instance.CurrentTimestamp);

            var callbackLogs = new List<string>();

            await AdvanceLoop(instance, callAdvanceBy, 10);
            Assert.Equal(10, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:cac4e224-15b6-45af-8df4-0a4d43b2ae05"));
            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:757d903d-376f-4e5f-accf-371fd5f06c3d"));
            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:245bd145-a538-49b8-b7c8-733f77e5d245"));

            await AdvanceLoop(instance, callAdvanceBy, 0);
            Assert.Equal(10, instance.CurrentTimestamp);
            Assert.Equal(new List<string>
            {
                "10:cac4e224-15b6-45af-8df4-0a4d43b2ae05",
                "10:757d903d-376f-4e5f-accf-371fd5f06c3d",
                "10:245bd145-a538-49b8-b7c8-733f77e5d245"
            }, callbackLogs);

            callbackLogs.Clear();
            await AdvanceLoop(instance, callAdvanceBy, 0);
            Assert.Equal(10, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:3978252e-188f-4f03-96e2-8036f13dfae2"), 5);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:e1e039a0-c83a-43da-8f29-81725eb7147f"), 6);
            var testTimeoutId = instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:ebf9dd1d-7157-420a-ac16-00a3fde9bf4e"), 11);
            Assert.NotNull(testTimeoutId);

            await AdvanceLoop(instance, callAdvanceBy, 4);
            Assert.Equal(14, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            await AdvanceLoop(instance, callAdvanceBy, 1);
            Assert.Equal(15, instance.CurrentTimestamp);
            Assert.Equal(new List<string>
            {
                "15:3978252e-188f-4f03-96e2-8036f13dfae2"
            }, callbackLogs);

            callbackLogs.Clear();
            await AdvanceLoop(instance, callAdvanceBy, 1);
            Assert.Equal(16, instance.CurrentTimestamp);
            Assert.Equal(new List<string>
            {
                "16:e1e039a0-c83a-43da-8f29-81725eb7147f"
            }, callbackLogs);

            callbackLogs.Clear();
            await AdvanceLoop(instance, callAdvanceBy, 4);
            Assert.Equal(20, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            instance.ClearTimeout(testTimeoutId);
            // test repeated cancellation of same id doesn't cause problems.
            instance.ClearTimeout(testTimeoutId);

            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:6d3a5586-b81d-4ca5-880b-2b711881a14e"));
            testTimeoutId = instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:8722d9a6-a7d4-47fe-a6d4-eee624fb0740"), 3);
            Assert.NotNull(testTimeoutId);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:2f7deeb1-f857-4f29-82de-b4168133f093"), 4);
            var testTimeoutId2 = instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:42989f22-a6d1-48ff-a554-86f79e87321e"), 3);
            Assert.NotNull(testTimeoutId2);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:9b463fec-6a9c-44cc-8165-e106080b18fc"), 0);
            var testImmediateId = instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:56805433-1f02-4327-b190-50862c0ba93e"));
            Assert.NotNull(testImmediateId);

            Assert.Empty(callbackLogs);

            // check whether wrong cancellation call will be ignored.
            instance.ClearTimeout(testImmediateId);

            await AdvanceLoop(instance, callAdvanceBy, 2);
            Assert.Equal(new List<string>
            {
                "20:6d3a5586-b81d-4ca5-880b-2b711881a14e",
                "20:9b463fec-6a9c-44cc-8165-e106080b18fc",
                "20:56805433-1f02-4327-b190-50862c0ba93e"
            }, callbackLogs);

            callbackLogs.Clear();
            instance.ClearTimeout(testTimeoutId);
            await AdvanceLoop(instance, callAdvanceBy, 3);
            Assert.Equal(25, instance.CurrentTimestamp);
            Assert.Equal(new List<string>
            {
                "23:42989f22-a6d1-48ff-a554-86f79e87321e",
                "24:2f7deeb1-f857-4f29-82de-b4168133f093",
            }, callbackLogs);

            callbackLogs.Clear();
            instance.ClearTimeout(testTimeoutId);

            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:6d3a5586-b81d-4ca5-880b-2b711881a14e"));
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:8722d9a6-a7d4-47fe-a6d4-eee624fb0740"), 3);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:2f7deeb1-f857-4f29-82de-b4168133f093"), 4);
            testTimeoutId = instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:42989f22-a6d1-48ff-a554-86f79e87321e"), 3);
            Assert.NotNull(testTimeoutId);

            // check whether wrong cancellation call will be ignored.
            instance.ClearImmediate(testTimeoutId);

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:9b463fec-6a9c-44cc-8165-e106080b18fc"), 0);
            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:56805433-1f02-4327-b190-50862c0ba93e"));
            testImmediateId = instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:5f08ae56-f596-4703-a9ab-3a66c6c29c07"));
            Assert.Empty(callbackLogs);

            Assert.Equal(7, instance.PendingEventCount);
            instance.ClearImmediate(testImmediateId);
            Assert.Equal(6, instance.PendingEventCount);

            await AdvanceLoop(instance, callAdvanceBy, 5);
            Assert.Equal(30, instance.CurrentTimestamp);
            Assert.Equal(new List<string>
            {
                "25:6d3a5586-b81d-4ca5-880b-2b711881a14e",
                "25:9b463fec-6a9c-44cc-8165-e106080b18fc",
                "25:56805433-1f02-4327-b190-50862c0ba93e",
                "28:8722d9a6-a7d4-47fe-a6d4-eee624fb0740",
                "28:42989f22-a6d1-48ff-a554-86f79e87321e",
                "29:2f7deeb1-f857-4f29-82de-b4168133f093"
            }, callbackLogs);

            callbackLogs.Clear();
            instance.ClearImmediate(testImmediateId); // test already used immediate cancellation isn't a problem.
            instance.ClearTimeout(testTimeoutId); // test already used timeout cancellation isn't a problem.
            instance.ClearTimeout(testTimeoutId2);  // test already used timeout isn't a problem.
            instance.ClearTimeout(null);  // test unexpected doesn't cause problems.
            instance.ClearImmediate(null);
            instance.ClearTimeout("jal");  // test unexpected doesn't cause problems.
            instance.ClearImmediate(3);

            await AdvanceLoop(instance, callAdvanceBy, 5);
            Assert.Equal(35, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            Assert.Equal(0, instance.PendingEventCount);
        }

        private Task AdvanceLoop(VirtualTimeBasedEventLoopApi instance, bool callAdvanceBy, int delay)
        {
            if (callAdvanceBy)
            {
                return instance.AdvanceTimeBy(delay);
            }
            else
            {
                return instance.AdvanceTimeTo(instance.CurrentTimestamp + delay);
            }
        }

        [Fact]
        public async Task TestNestedCallbackPosts()
        {
            var instance = new VirtualTimeBasedEventLoopApi();

            Assert.Equal(0, instance.CurrentTimestamp);

            var callbackLogs = new List<string>();

            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:cac4e224-15b6-45af-8df4-0a4d43b2ae05"));
            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:757d903d-376f-4e5f-accf-371fd5f06c3d"));
            instance.SetImmediate(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:245bd145-a538-49b8-b7c8-733f77e5d245"));

            await instance.AdvanceTimeBy(0);
            Assert.Equal(0, instance.CurrentTimestamp);
            Assert.Equal(new List<string>
            {
                "0:cac4e224-15b6-45af-8df4-0a4d43b2ae05",
                "0:757d903d-376f-4e5f-accf-371fd5f06c3d",
                "0:245bd145-a538-49b8-b7c8-733f77e5d245"
            }, callbackLogs);

            callbackLogs.Clear();
            await instance.AdvanceTimeBy(0);
            Assert.Equal(0, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            instance.SetTimeout(() =>
            {
                callbackLogs.Add($"{instance.CurrentTimestamp}:3978252e-188f-4f03-96e2-8036f13dfae2");
                instance.SetTimeout(() =>
                    callbackLogs.Add($"{instance.CurrentTimestamp}:240fbcc0-9930-4e96-9b62-356458ee0a9f"), 4);
            }, 5);

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:e1e039a0-c83a-43da-8f29-81725eb7147f"), 6);

            // test that in the absence of any extra setting, async work
            // will cause timestamp supplied at callback execution to be different
            // from the logged value.
            var tcs = new TaskCompletionSource<object>();
            Func<Task> asyncWork = async () =>
            {
                await Task.Delay(1_000);
                callbackLogs.Add($"{instance.CurrentTimestamp}:ebf9dd1d-7157-420a-ac16-00a3fde9bf4e");
                tcs.SetResult(null);
            };
            instance.SetTimeout(() =>
            {
                _ = asyncWork.Invoke();
            }, 11);

            await instance.AdvanceTimeTo(14);
            Assert.Equal(14, instance.CurrentTimestamp);

            await tcs.Task;
            Assert.Equal(0, instance.PendingEventCount);
            // check that all but last log is as expected.
            Assert.Equal(new List<string>
            {
                "5:3978252e-188f-4f03-96e2-8036f13dfae2",
                "6:e1e039a0-c83a-43da-8f29-81725eb7147f",
                "9:240fbcc0-9930-4e96-9b62-356458ee0a9f"
            }, callbackLogs.GetRange(0, 3));
            Assert.NotEqual("11:ebf9dd1d-7157-420a-ac16-00a3fde9bf4e", callbackLogs[3]);

            callbackLogs.Clear();
            await instance.AdvanceTimeTo(4); // test backward movement of time.
            Assert.Equal(4, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            // test long async work with callback aftermath delays, and
            // ensure async work passes equality check this time.
            asyncWork = async () =>
            {
                instance.PushCallbackAftermathDelay(() => Task.Delay(1_200));
                try
                {
                    await Task.Delay(1_000);
                    callbackLogs.Add($"{instance.CurrentTimestamp}:c74feb30-7e58-4e47-956b-f4ce5f3fc32c");
                    instance.SetTimeout(async () =>
                    {
                        instance.PushCallbackAftermathDelay(() => Task.Delay(700));
                        try
                        {
                            await Task.Delay(500);
                            callbackLogs.Add($"{instance.CurrentTimestamp}:b180111d-3179-4c50-9006-4a7591f05640");
                        }
                        finally
                        {
                            instance.PopCallbackAftermathDelay();
                        }
                    }, 7);
                }
                finally
                {
                    instance.PopCallbackAftermathDelay();
                }
            };
            instance.SetTimeout(() =>
            {
                _ = asyncWork.Invoke();
            }, 3);

            await instance.AdvanceTimeTo(20);
            Assert.Equal(new List<string>
            {
                "7:c74feb30-7e58-4e47-956b-f4ce5f3fc32c",
                "14:b180111d-3179-4c50-9006-4a7591f05640"
            }, callbackLogs);
            Assert.Equal(0, instance.PendingEventCount);

            callbackLogs.Clear();
            await instance.AdvanceTimeTo(0);
            Assert.Equal(0, instance.CurrentTimestamp);
            Assert.Empty(callbackLogs);

            Assert.Equal(0, instance.PendingEventCount);
        }

        [Fact]
        public async Task TestPerformanceForOverOneThousand()
        {
            var instance = new VirtualTimeBasedEventLoopApi();

            var timeLimit = 10_000;
            for (int i = 0; i < timeLimit; i++)
            {
                instance.SetTimeout(() => { }, i);
            }
            await instance.AdvanceTimeTo(timeLimit);
        }

        [Fact]
        public async Task TestForErrors()
        {
            var instance = new VirtualTimeBasedEventLoopApi();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AdvanceTimeBy(-1));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AdvanceTimeTo(-1));
            Assert.Throws<ArgumentNullException>(() =>
                instance.SetImmediate(null));
            Assert.Throws<ArgumentNullException>(() =>
                instance.SetTimeout(null, 0));
            Assert.Throws<ArgumentException>(() =>
                instance.SetTimeout(() => { }, -1));
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
        public async Task TestWhenSetImmediate()
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
                var result = instance.WhenSetImmediate(cb);
                related.Add(result.Item1);
                cancellationHandles.Add(result.Item2);

                // the rest should not execute.
                for (int j = 0; j < 2; j++)
                {
                    result = instance.WhenSetImmediate(async () =>
                    {
                        await cb.Invoke();
                    });
                    related.Add(result.Item1);
                    cancellationHandles.Add(result.Item2);
                }
                tasks.Add(related);
            }

            // this must finish executing after all previous tasks have executed.
            var lastOne = instance.WhenSetImmediate(() => Task.CompletedTask).Item1;

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
            ComparisonUtils.AssertLogsEqual(expected, actual, _outputHelper);
        }

        [Fact]
        public async Task TestWhenSetTimeout()
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
                var result = instance.WhenSetTimeout(timeoutValue, cb);
                related.Add(result.Item1);
                cancellationHandles.Add(result.Item2);

                // the rest should not execute.
                for (int j = 0; j < 2; j++)
                {
                    result = instance.WhenSetTimeout(timeoutValue, cb);
                    related.Add(result.Item1);
                    cancellationHandles.Add(result.Item2);
                }
                tasks.Add(related);
            }

            // this must finish executing after all previous tasks have executed.
            var lastOne = instance.WhenSetTimeout(5000);

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
            ComparisonUtils.AssertLogsEqual(expected, actual, _outputHelper);
        }

        [Fact]
        public async Task TestForDeadlockAvoidance()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
            var tcs = new TaskCompletionSource<object>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var laterTask = instance.WhenSetTimeout(1800, () =>
            {
                tcs.SetResult(null);
                return Task.CompletedTask;
            }).Item1;
            var dependentTask = instance.WhenSetTimeout(500, async () =>
            {
                await tcs.Task;
            }).Item1;

            // act
            await instance.AdvanceTimeTo(2_000);
            
            // assert completion.
            await dependentTask;
            await laterTask;
        }
    }
}
