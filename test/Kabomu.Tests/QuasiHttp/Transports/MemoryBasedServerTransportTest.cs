using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
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
            var expectedConnection = await instance.CreateConnectionForClient(null, null);
            var receiveConnectionResponse = await serverConnectTask;
            Assert.Equal(expectedConnection, receiveConnectionResponse.Connection);

            // test for sequential read/write request processing.
            var workItems1 = WorkItem.CreateWorkItems();
            await ProcessWorkItems(instance, expectedConnection, true, workItems1);
            var workItems2 = WorkItem.CreateWorkItems();
            await ProcessWorkItems(instance, expectedConnection, false, workItems2);

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
            var expectedMutex = new LockBasedMutexApi();
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, opt) =>
                {
                    Assert.Equal(expectedReq, req);
                    Assert.Equal(expectedMutex, opt.ProcessingMutexApi);
                    return Task.FromResult<IQuasiHttpResponse>(expectedRes);
                }
            };
            var instance = new MemoryBasedServerTransport
            {
                LocalEndpoint = "Accra",
                Application = app
            };
            var running = await instance.IsRunning();
            Assert.False(running);

            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ProcessDirectSendRequest("Tafo", expectedMutex, new DefaultQuasiHttpRequest());
            });

            await instance.Start();
            await instance.Start();
            running = await instance.IsRunning();
            Assert.True(running);

            var res = await instance.ProcessDirectSendRequest("Accra",
                expectedMutex, expectedReq);
            Assert.Equal(expectedRes, res);

            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ProcessDirectSendRequest("Tafo", expectedMutex, null);
            });

            Task<IConnectionAllocationResponse> serverConnectTask;
            Task<object> clientConnectTask;
            if (connectToClientFirst)
            {
                clientConnectTask = instance.CreateConnectionForClient("Kumasi", null);
                serverConnectTask = instance.ReceiveConnection();
            }
            else
            {
                serverConnectTask = instance.ReceiveConnection();
                clientConnectTask = instance.CreateConnectionForClient("Kumasi", null);
            }

            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(serverConnectTask, clientConnectTask);
            await Task.WhenAll(serverConnectTask, clientConnectTask);

            var expectedConnection = await clientConnectTask;
            var receiveConnectionResponse = await serverConnectTask;
            Assert.Equal(expectedConnection, receiveConnectionResponse.Connection);

            // test for interleaved read/write request processing.
            var workItems1 = WorkItem.CreateWorkItems();
            var workItems2 = WorkItem.CreateWorkItems();
            var task1 = ProcessWorkItems(instance, expectedConnection, true, workItems1);
            var task2 = ProcessWorkItems(instance, expectedConnection, false, workItems2);
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(task1, task2);
            await Task.WhenAll(task1, task2);

            // test that release connection works.
            var exTask1 = instance.ReadBytes(expectedConnection, new byte[2], 0, 2);
            var exTask2 = instance.WriteBytes(expectedConnection, new byte[3], 1, 2);
            await instance.ReleaseConnection(expectedConnection);
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
