using Kabomu.QuasiHttp;
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
        public async Task TestSequentialOperations()
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
            var expectedConnectionResponse = await instance.CreateConnectionForClient(null, null);
            var receiveConnectionResponse = await serverConnectTask;
            Assert.Equal(expectedConnectionResponse.Connection, receiveConnectionResponse.Connection);

            var establishedConnection = receiveConnectionResponse.Connection;

            // test for sequential read/write request processing.
            var workItems1 = WorkItem.CreateWorkItems();
            await ProcessWorkItems(instance, establishedConnection, true, workItems1);
            var workItems2 = WorkItem.CreateWorkItems();
            await ProcessWorkItems(instance, establishedConnection, false, workItems2);

            // test that release connection works.
            var exTask1 = instance.ReadBytes(establishedConnection, new byte[2], 0, 2);
            var exTask2 = instance.WriteBytes(establishedConnection, new byte[3], 1, 2);
            await instance.ReleaseConnection(establishedConnection);
            await Assert.ThrowsAnyAsync<Exception>(() => exTask1);
            await Assert.ThrowsAnyAsync<Exception>(() => exTask2);

            // test that all attempts to read leads to exceptions.
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ReadBytes(establishedConnection, new byte[1], 0, 1);
            });
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.WriteBytes(establishedConnection, new byte[1], 0, 1);
            });

            // test that repeated call doesn't have effect.
            await instance.ReleaseConnection(establishedConnection);

            await instance.Stop();
            await instance.Stop();
            running = await instance.IsRunning();
            Assert.False(running);
        }

        [Fact]
        public async Task TestErrorUsage()
        {
            var instance = new MemoryBasedServerTransport();
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.CreateConnectionForClient(null, null);
            });
            
            await instance.Start();

            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.ReadBytes(null, new byte[0], 0, 0);
            });
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ReadBytes(4, new byte[2], 0, 1);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.ReadBytes(new MemoryBasedTransportConnectionInternal(null, null), new byte[1], 1, 1);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.WriteBytes(null, new byte[0], 0, 0);
            });
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.WriteBytes(4, new byte[2], 0, 1);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.WriteBytes(new MemoryBasedTransportConnectionInternal(null, null), new byte[1], 1, 1);
            });
        }

        [Fact]
        public async Task TestInterleavedOperations()
        {
            var task1 = ForkOperations(false);
            var task2 = ForkOperations(true);
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(task1, task2);
            await Task.WhenAll(task1, task2);
        }

        private async Task ForkOperations(bool connectToClientFirst)
        {
            var expectedReq = new DefaultQuasiHttpRequest();
            var expectedRes = new DefaultQuasiHttpResponse();
            
            var instance = new MemoryBasedServerTransport();
            var running = await instance.IsRunning();
            Assert.False(running);

            await instance.Start();
            await instance.Start();
            running = await instance.IsRunning();
            Assert.True(running);

            Task<IConnectionAllocationResponse> serverConnectTask;
            Task<IConnectionAllocationResponse> clientConnectTask;
            if (connectToClientFirst)
            {
                clientConnectTask = instance.CreateConnectionForClient("Accra", "Kumasi");
                serverConnectTask = instance.ReceiveConnection();
            }
            else
            {
                serverConnectTask = instance.ReceiveConnection();
                clientConnectTask = instance.CreateConnectionForClient("Accra", "Kumasi");
            }

            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(serverConnectTask, clientConnectTask);
            await Task.WhenAll(serverConnectTask, clientConnectTask);

            var expectedConnectionResponse = await clientConnectTask;
            var receiveConnectionResponse = await serverConnectTask;
            Assert.Equal(expectedConnectionResponse.Connection, receiveConnectionResponse.Connection);

            var establishedConnection = receiveConnectionResponse.Connection;

            // test for interleaved read/write request processing.
            var workItems1 = WorkItem.CreateWorkItems();
            var workItems2 = WorkItem.CreateWorkItems();
            var task1 = ProcessWorkItems(instance, establishedConnection, true, workItems1);
            var task2 = ProcessWorkItems(instance, establishedConnection, false, workItems2);
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(task1, task2);
            await Task.WhenAll(task1, task2);

            // test that release connection works.
            var exTask1 = instance.ReadBytes(establishedConnection, new byte[2], 0, 2);
            var exTask2 = instance.WriteBytes(establishedConnection, new byte[3], 1, 2);
            await instance.ReleaseConnection(establishedConnection);
            await Assert.ThrowsAnyAsync<Exception>(() => exTask1);
            await Assert.ThrowsAnyAsync<Exception>(() => exTask2);

            await instance.Stop();
            await instance.Stop();
            running = await instance.IsRunning();
            Assert.False(running);
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
    }
}
