using Kabomu.Concurrency;
using Kabomu.MemoryBasedTransport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.MemoryBasedTransport
{
    public class MemoryPipeBackedBodyTest
    {
        [Fact]
        public async Task TestEmptyRead()
        {
            // arrange.
            var instance = new MemoryPipeBackedBody();
            var task = instance.WriteLastBytes(new byte[0], 0, 0);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, null,
                new int[0], null, new byte[0]);
            //Assert.True(task.IsCompleted); // observed to be sometimes false during tests
            await task;
        }

        [Fact]
        public async Task TestNonEmptyRead()
        {
            // arrange.
            var instance = new MemoryPipeBackedBody
            {
                ContentType = "text/csv" 
            };
            var tasks = new Task[2];
            tasks[0] = instance.WriteBytes(new byte[] { (byte)'A', (byte)'b' }, 0, 2);
            tasks[1] = instance.WriteLastBytes(new byte[] { (byte)'2' }, 0, 1);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "text/csv",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
            // could omit WhenAny (equivalent to JS promise.race), but it is there so as to detect
            // any errors which can cause one of the tasks to hang forever.
            await await Task.WhenAny(tasks);
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task TestNonEmptyRead2()
        {
            // arrange.
            var expectedData = Encoding.UTF8.GetBytes("car seat");
            var instance = new MemoryPipeBackedBody
            {
                ContentType = "text/xml",
                MutexApi = new DefaultEventLoopApi()
            };
            var tasks = new Task[expectedData.Length];
            for (int i = 0; i < expectedData.Length; i++)
            {
                if (i == expectedData.Length - 1)
                {
                    tasks[i] = instance.WriteLastBytes(expectedData, i, 1);
                }
                else
                {
                    tasks[i] = instance.WriteBytes(expectedData, i, 1);
                }
            }

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(1, instance, -1, "text/xml",
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 }, null, expectedData);
        }

        [Fact]
        public async Task TestForArgumentErrors()
        {
            var instance = new MemoryPipeBackedBody();
            _ = instance.WriteLastBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2);
            await CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);

            // look out for specific errors.
            instance = new MemoryPipeBackedBody();
            var writeTasks = new Task[4];
            var expectedWriteErrors = new string[writeTasks.Length];
            var readTasks = new Task<int>[4];
            var expectedReadErrors = new string[readTasks.Length];
            var expectedReadLengths = new int[readTasks.Length];
            readTasks[0] = instance.ReadBytes(new byte[4], 0, 4);
            expectedReadLengths[0] = 2;
            readTasks[1] = instance.ReadBytes(new byte[4], 0, 2);
            expectedReadLengths[1] = 1;
            readTasks[2] = instance.ReadBytes(new byte[4], 0, 2);
            expectedReadLengths[2] = 0;
            writeTasks[0] = instance.WriteBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2);
            expectedWriteErrors[0] = null;
            writeTasks[1] = instance.WriteLastBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 1);
            expectedWriteErrors[1] = null;
            writeTasks[2] = instance.WriteLastBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2);
            expectedWriteErrors[2] = "end of write";
            writeTasks[3] = instance.WriteBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2);
            expectedWriteErrors[3] = "end of write";
            readTasks[3] = instance.ReadBytes(new byte[4], 0, 4);
            expectedReadLengths[3] = 0;

            // wait for all tasks to complete.
            // since c#'s when all behaves more like NodeJS's Promise.allSettle,
            // use continuations to avoid dealing with any expected or unexpected errors
            await Task.WhenAll(writeTasks.Select(t => t.ContinueWith(t => { })));
            await Task.WhenAll(readTasks.Select(t => t.ContinueWith(t => { })));

            // assert.
            for (int i = 0; i < readTasks.Length; i++)
            {
                var task = readTasks[i];
                var expectedError = expectedReadErrors[i];
                if (expectedError != null)
                {
                    Assert.False(task.IsCompletedSuccessfully);
                    Assert.Contains(expectedError, task.Exception.Message);
                }
                else
                {
                    if (!task.IsCompletedSuccessfully)
                    {
                        Assert.True(task.IsCompletedSuccessfully, "Didn't expect: " + task.Exception);
                    }
                    Assert.Equal(expectedReadLengths[i], task.Result);
                }
            }
            for (int i = 0; i < writeTasks.Length; i++)
            {
                var task = writeTasks[i];
                var expectedError = expectedWriteErrors[i];
                if (expectedError != null)
                {
                    Assert.False(task.IsCompletedSuccessfully);
                    Assert.Contains(expectedError, task.Exception.Message);
                }
                else
                {
                    if (!task.IsCompletedSuccessfully)
                    {
                        Assert.True(task.IsCompletedSuccessfully, "Didn't expect: " + task.Exception);
                    }
                }
            }
        }
    }
}
