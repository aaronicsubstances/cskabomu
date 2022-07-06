using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.Transports
{
    public class MemoryBasedServerTransportTest
    {
        [Fact]
        public async Task TestOperations()
        {
            var instance = new MemoryBasedServerTransport();
            var running = await instance.IsRunning();
            Assert.False(running);
            await instance.Start();
            running = await instance.IsRunning();
            Assert.True(running);
            await instance.Stop();
            running = await instance.IsRunning();
            Assert.False(running);

            await instance.Start();
            await instance.Start();
            running = await instance.IsRunning();
            Assert.True(running);

            var serverConnectTask = instance.ReceiveConnection();
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ReceiveConnection();
            });
            var expectedConnection = await instance.CreateConnectionForClient(null, null);
            var receiveConnectionResponse = await serverConnectTask;
            Assert.Equal(expectedConnection, receiveConnectionResponse.Connection);

            // test for sequential read/write request processing.
            var workItems1 = CreateWorkItems();
            await ProcessWorkItems(instance, expectedConnection, true, workItems1);
            var workItems2 = CreateWorkItems();
            await ProcessWorkItems(instance, expectedConnection, false, workItems2);
            
            // test for interleaved read/write request processing.
            workItems1 = CreateWorkItems();
            workItems2 = CreateWorkItems();
            var task1 = ProcessWorkItems(instance, expectedConnection, true, workItems1);
            var task2 = ProcessWorkItems(instance, expectedConnection, false, workItems2);
            // use whenAny to check for any exceptions.
            await await Task.WhenAny(task1, task2);
            await Task.WhenAll(task1, task2);

            // test that release connection works.
            var exTask1 = instance.ReadBytes(expectedConnection, new byte[2], 0, 2);
            var exTask2 = instance.WriteBytes(expectedConnection, new byte[3], 1, 2);
            await instance.ReleaseConnection(expectedConnection);
            await Assert.ThrowsAnyAsync<Exception>(() => exTask1);
            await Assert.ThrowsAnyAsync<Exception>(() => exTask2);

            // test that all attempts to read leads to exceptions.
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ReadBytes(expectedConnection, new byte[1], 0, 1);
            });
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.WriteBytes(expectedConnection, new byte[1], 0, 1);
            });

            // test that repeated call doesn't have effect.
            await instance.ReleaseConnection(expectedConnection);

            await instance.Stop();
            await instance.Stop();
            running = await instance.IsRunning();
            Assert.False(running);
        }

        private List<WorkItem> CreateWorkItems()
        {
            var workItems = new List<WorkItem>();

            var writeData = ByteUtils.StringToBytes("ehlo from acres");
            var readData = new byte[34];
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 0,
                WriteLength = writeData.Length,
                Dependency = "0159b309-9224-4024-89f4-a90d66bf9f3c"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 0,
                BytesToRead = readData.Length,
                ExpectedBytesRead = writeData.Length,
                ExpectedReadData = writeData,
                Tag = "0159b309-9224-4024-89f4-a90d66bf9f3c"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = new byte[0],
                ExpectedReadData = new byte[0]
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = new byte[0]
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 20,
                BytesToRead = 10,
                ExpectedBytesRead = 10,
                ExpectedReadData = ByteUtils.StringToBytes("ehlo from "),
                Dependency = "184e0a48-ff97-472c-9b8c-f84a4cccc637"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 0,
                WriteLength = writeData.Length,
                Tag = "184e0a48-ff97-472c-9b8c-f84a4cccc637",
                Dependency = "cfefe0d8-2578-4376-a613-1819c5a99446"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 2,
                BytesToRead = 1,
                ExpectedBytesRead = 1,
                ExpectedReadData = ByteUtils.StringToBytes("a")
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 0,
                WriteLength = 4,
                Dependency = "f2547a5c-0e24-4ba4-91a5-128f9ef4eb27"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = true,
                WriteData = writeData,
                WriteOffset = 10,
                WriteLength = 5,
                Dependency = "83e659d1-0152-4a2d-a9c1-d1732a99325b"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 20,
                BytesToRead = 10,
                ExpectedBytesRead = 4,
                ExpectedReadData = ByteUtils.StringToBytes("cres"),
                Tag = "cfefe0d8-2578-4376-a613-1819c5a99446"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 0,
                BytesToRead = readData.Length,
                ExpectedBytesRead = 4,
                ExpectedReadData = ByteUtils.StringToBytes("ehlo"),
                Tag = "f2547a5c-0e24-4ba4-91a5-128f9ef4eb27"
            });
            workItems.Add(new WorkItem
            {
                IsWrite = false,
                ReadBuffer = readData,
                ReadOffset = 0,
                BytesToRead = readData.Length,
                ExpectedBytesRead = 5,
                ExpectedReadData = ByteUtils.StringToBytes("acres"),
                Tag = "83e659d1-0152-4a2d-a9c1-d1732a99325b"
            });
            return workItems;
        }

        private async Task ProcessWorkItems(MemoryBasedServerTransport server,
            object connection, bool writeToServer, List<WorkItem> workItems)
        {
            var uncompletedWorkItems = new List<WorkItem>();
            foreach (var pendingWork in workItems)
            {
                if (pendingWork.IsWrite)
                {
                    if (writeToServer)
                    {
                        pendingWork.WriteTask = server.WriteBytes(connection,
                            pendingWork.WriteData, pendingWork.WriteOffset, pendingWork.WriteLength);
                    }
                    else
                    {
                        pendingWork.WriteTask = MemoryBasedServerTransport.WriteBytesInternal(false, connection,
                            pendingWork.WriteData, pendingWork.WriteOffset, pendingWork.WriteLength);
                    }
                }
                else
                {
                    if (writeToServer)
                    {
                        pendingWork.ReadTask = MemoryBasedServerTransport.ReadBytesInternal(false, connection,
                            pendingWork.ReadBuffer, pendingWork.ReadOffset, pendingWork.BytesToRead);
                    }
                    else
                    {
                        pendingWork.ReadTask = server.ReadBytes(connection,
                            pendingWork.ReadBuffer, pendingWork.ReadOffset, pendingWork.BytesToRead);
                    }
                }
                uncompletedWorkItems.Add(pendingWork);

                // check whether there are uncompleted tasks which
                // can now be completed because their dependency has been met.
                for (int j = 0; j < uncompletedWorkItems.Count; j++)
                {
                    var uncompletedWork = uncompletedWorkItems[j];
                    if (uncompletedWork == null)
                    {
                        continue;
                    }
                    if (uncompletedWork.Dependency != null &&
                        uncompletedWork.Dependency != pendingWork.Tag)
                    {
                        continue;
                    }

                    // eject.
                    uncompletedWorkItems[j] = null;

                    // await tasks and run assertions.
                    if (uncompletedWork.IsWrite)
                    {
                        await uncompletedWork.WriteTask;
                    }
                    else
                    {
                        int actualBytesRead = await uncompletedWork.ReadTask;
                        Assert.True(actualBytesRead <= uncompletedWork.BytesToRead);
                        Assert.Equal(uncompletedWork.ExpectedBytesRead, actualBytesRead);

                        byte[] actual = new byte[actualBytesRead];
                        Array.Copy(uncompletedWork.ReadBuffer, uncompletedWork.ReadOffset, actual, 0, actual.Length);

                        Assert.Equal(uncompletedWork.ExpectedReadData, actual);
                    }
                }
            }

            foreach (var work in workItems)
            {
                if (work.IsWrite)
                {
                    Assert.True(work.WriteTask.IsCompleted);
                }
                else
                {
                    Assert.True(work.ReadTask.IsCompleted);
                }
            }
        }

        class WorkItem
        {
            public bool IsWrite { get; set; }
            public string Dependency { get; set; }
            public string Tag { get; set; }
            public byte[] WriteData { get; set; }
            public int WriteOffset { get; set; }
            public int WriteLength { get; set; }
            public byte[] ReadBuffer { get; set; }
            public int ReadOffset { get; set; }
            public int BytesToRead { get; set; }
            public int ExpectedBytesRead { get; set; }
            public byte[] ExpectedReadData { get; set; }
            public Task WriteTask { get; set; }
            public Task<int> ReadTask { get; set; }
        }
    }
}
