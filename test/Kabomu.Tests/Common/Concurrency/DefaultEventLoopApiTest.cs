using Kabomu.Common.Concurrency;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Kabomu.Tests.Common.Concurrency
{
    public class DefaultEventLoopApiTest
    {
        private readonly ITestOutputHelper _outputHelper;

        public DefaultEventLoopApiTest(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task TestSetImmediate()
        {
            // arrange.
            var instance = new DefaultEventLoopApi();
            var expected = new List<string>();
            var actual = new List<string>();
            var tasks = new List<Task<int>>();
            int i;
            for (i = 0; i < 100; i++)
            {
                var capturedIndex = i;
                expected.Add("" + capturedIndex);
                var cancellationTokenSource = new CancellationTokenSource();
                Func<Task<int>> cb = () =>
                {
                    cancellationTokenSource.Cancel();
                    actual.Add("" + capturedIndex);
                    return Task.FromResult(capturedIndex);
                };
                tasks.Add(instance.SetImmediate(CancellationToken.None, cb));
                _ = instance.SetImmediate(cancellationTokenSource.Token, cb);
                _ = instance.SetImmediate(cancellationTokenSource.Token, async () =>
                {
                    await cb.Invoke();
                });
            }

            // this should finish executing after all previous tasks have executed.
            await instance.SetImmediate(CancellationToken.None, () => Task.CompletedTask);

            // assert
            // check for correct return values.
            i = 0;
            foreach (var task in tasks)
            {
                Assert.True(task.IsCompleted);
                if (!task.IsCompletedSuccessfully)
                {
                    Assert.True(task.IsCompletedSuccessfully, "Didn't expect: " + task.Exception);
                }
                Assert.Equal(i, task.Result);
                i++;
            }

            // finally ensure correct ordering of execution of tasks.
            new OutputEventLogger { Logs = actual }.AssertEqual(expected, _outputHelper);
        }

        [Fact]
        public async Task TestSetTimeout()
        {
            // arrange.
            var instance = new DefaultEventLoopApi();
            var expected = new List<string>();
            var actual = new List<string>();
            int i;
            for (i = 0; i < 50; i++)
            {
                var capturedIndex = i;
                expected.Add("" + capturedIndex);
                var cancellationTokenSource = new CancellationTokenSource();
                Func<Task<int>> cb = () =>
                {
                    cancellationTokenSource.Cancel();
                    actual.Add("" + capturedIndex);
                    return Task.FromResult(capturedIndex);
                };
                // Since it is not deterministic as to which call to setTimeout will execute first,
                // race multiple tasks with cancellation.
                // Also 50 ms is more than enough to distinguish callback firing times
                // on the common operating systems (15ms max on Windows, 10ms max on Linux).
                int timeoutValue = 50 * i + 500;
                _ = instance.SetTimeout(timeoutValue, cancellationTokenSource.Token, cb);
                _ = instance.SetTimeout(timeoutValue, cancellationTokenSource.Token, cb);
                _ = instance.SetTimeout(timeoutValue, cancellationTokenSource.Token, async () =>
                {
                    await cb.Invoke();
                });
            }

            Task task1 = instance.SetTimeout(3000, CancellationToken.None, () => Task.CompletedTask);
            Task<int> task2 = instance.SetTimeout(3100, CancellationToken.None, () => Task.FromResult(177));
            // this should finish executing after all previous tasks have executed.
            await task1;
            int finalRes = await task2;

            // assert
            Assert.Equal(177, finalRes);

            // finally ensure correct ordering of execution of tasks.
            new OutputEventLogger { Logs = actual }.AssertEqual(expected, _outputHelper);
        }
    }
}
