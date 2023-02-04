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

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:cac4e224-15b6-45af-8df4-0a4d43b2ae05"), 0);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:757d903d-376f-4e5f-accf-371fd5f06c3d"), 0);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:245bd145-a538-49b8-b7c8-733f77e5d245"), 0);

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

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:6d3a5586-b81d-4ca5-880b-2b711881a14e"), 0);
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

            Assert.Empty(callbackLogs);

            await AdvanceLoop(instance, callAdvanceBy, 2);
            Assert.Equal(new List<string>
            {
                "20:6d3a5586-b81d-4ca5-880b-2b711881a14e",
                "20:9b463fec-6a9c-44cc-8165-e106080b18fc"
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

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:6d3a5586-b81d-4ca5-880b-2b711881a14e"), 0);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:8722d9a6-a7d4-47fe-a6d4-eee624fb0740"), 3);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:2f7deeb1-f857-4f29-82de-b4168133f093"), 4);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:42989f22-a6d1-48ff-a554-86f79e87321e"), 3);

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:9b463fec-6a9c-44cc-8165-e106080b18fc"), 0);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:56805433-1f02-4327-b190-50862c0ba93e"), 0);
            testTimeoutId = instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:5f08ae56-f596-4703-a9ab-3a66c6c29c07"), 0);
            Assert.NotNull(testTimeoutId);
            Assert.Empty(callbackLogs);

            Assert.Equal(7, instance.PendingEventCount);
            instance.ClearTimeout(testTimeoutId);
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
            instance.ClearTimeout(testTimeoutId); // test already used timeout cancellation isn't a problem.
            instance.ClearTimeout(testTimeoutId2);  // test already used timeout isn't a problem.
            instance.ClearTimeout(null);  // test unexpected doesn't cause problems.
            instance.ClearTimeout("jal");  // test unexpected doesn't cause problems.

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

            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:cac4e224-15b6-45af-8df4-0a4d43b2ae05"), 0);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:757d903d-376f-4e5f-accf-371fd5f06c3d"), 0);
            instance.SetTimeout(() =>
                callbackLogs.Add($"{instance.CurrentTimestamp}:245bd145-a538-49b8-b7c8-733f77e5d245"), 0);

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
            instance.DefaultCallbackAftermathDelayance = () => Task.Delay(700);
            asyncWork = async () =>
            {
                instance.StickyCallbackAftermathDelayance = () => Task.Delay(1_200);

                await Task.Delay(1_000);
                callbackLogs.Add($"{instance.CurrentTimestamp}:c74feb30-7e58-4e47-956b-f4ce5f3fc32c");
                instance.SetTimeout(async () =>
                {
                    await Task.Delay(500);
                    callbackLogs.Add($"{instance.CurrentTimestamp}:b180111d-3179-4c50-9006-4a7591f05640");
                }, 7);
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

            // reset default delayance.
            instance.DefaultCallbackAftermathDelayance = null;

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
            Assert.Throws<ArgumentNullException>(() =>
                instance.SetTimeout(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.SetTimeout(() => { }, -1));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                instance.AdvanceTimeBy(-1));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                instance.AdvanceTimeTo(-1));
        }

        [Fact]
        public async Task TestDelayWithDeadlockAvoidance()
        {
            // arrange.
            var instance = new VirtualTimeBasedEventLoopApi();
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
            await instance.AdvanceTimeTo(2_000);
            
            // assert completion.
            await dependentTask;
            await laterTask;
        }
    }
}
