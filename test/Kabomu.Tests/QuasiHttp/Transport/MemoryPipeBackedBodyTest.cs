using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Transport
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
            var writeTasks = new List<Tuple<Task, string>>();
            var readTasks = new List<Tuple<Task<int>, string, int>>();

            readTasks.Add(Tuple.Create<Task<int>, string, int>(
                instance.ReadBytes(new byte[4], 0, 4), null, 2));
            readTasks.Add(Tuple.Create<Task<int>, string, int>(
                instance.ReadBytes(new byte[4], 0, 2), null, 1));
            readTasks.Add(Tuple.Create<Task<int>, string, int>(
                instance.ReadBytes(new byte[4], 0, 2), null, 0));
            writeTasks.Add(Tuple.Create<Task, string>(
                instance.WriteBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2), null));
            writeTasks.Add(Tuple.Create<Task, string>(
                instance.WriteLastBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 1), null));
            writeTasks.Add(Tuple.Create<Task, string>(
                instance.WriteLastBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2), "end of write"));
            writeTasks.Add(Tuple.Create<Task, string>(
                instance.WriteBytes(new byte[] { (byte)'c', (byte)'2' }, 0, 2), "end of write"));
            readTasks.Add(Tuple.Create<Task<int>, string, int>(
                instance.ReadBytes(new byte[4], 0, 4), null, 0));

            // wait for all tasks to complete.
            // since c#'s when all behaves more like NodeJS's Promise.allSettle,
            // use continuations to avoid dealing with any expected or unexpected errors
            await Task.WhenAll(writeTasks.Select(t => t.Item1.ContinueWith(t => { })));
            await Task.WhenAll(readTasks.Select(t => t.Item1.ContinueWith(t => { })));

            await instance.EndRead(new Exception("custom end error works"));

            readTasks.Add(Tuple.Create<Task<int>, string, int>(
                instance.ReadBytes(new byte[1], 0, 1), "custom end error works", 0));
            writeTasks.Add(Tuple.Create<Task, string>(
                instance.WriteBytes(new byte[1], 0, 1), "custom end error works"));

            // assert.
            foreach (var item in readTasks)
            {
                var task = item.Item1;
                var expectedError = item.Item2;
                var expectedReadLength = item.Item3;
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
                    Assert.Equal(expectedReadLength, task.Result);
                }
            }
            foreach (var item in writeTasks)
            {
                var task = item.Item1;
                var expectedError = item.Item2;
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
