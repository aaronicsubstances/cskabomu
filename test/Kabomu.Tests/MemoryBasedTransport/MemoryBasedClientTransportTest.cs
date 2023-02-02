using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.MemoryBasedTransport
{
    public class MemoryBasedClientTransportTest
    {
        [Fact]
        public async Task TestSequentialOperations()
        {
            var hub = new TestHub();
            var instance = new MemoryBasedClientTransport
            {
                Hub = hub
            };

            instance.LocalEndpoint = "Abuja";
            hub.ExpectedClient = instance;
            hub.ExpectedConnectivityParams = new DefaultConnectivityParams();
            var connectionResponse = await instance.AllocateConnection(hub.ExpectedConnectivityParams);
            Assert.True(connectionResponse.Connection is MemoryBasedTransportConnectionInternal);

            var establishedConnection = connectionResponse.Connection;

            // test for sequential read/write request processing.
            var workItems1 = WorkItem.CreateWorkItems();
            await ProcessWorkItems(instance, establishedConnection, true, workItems1);
            var workItems2 = WorkItem.CreateWorkItems();
            await ProcessWorkItems(instance, establishedConnection, false, workItems2);

            // test that release connection works.
            var exTask1 = instance.ReadBytes(establishedConnection, new byte[2], 0, 2);
            var exTask2 = instance.WriteBytes(establishedConnection, new byte[3], 1, 2);
            await instance.ReleaseConnection(establishedConnection);
            await Assert.ThrowsAsync<ConnectionReleasedException>(() => exTask1);
            await Assert.ThrowsAsync<ConnectionReleasedException>(() => exTask2);

            // test that all attempts to read leads to exceptions.
            await Assert.ThrowsAsync<ConnectionReleasedException>(() =>
            {
                return instance.ReadBytes(establishedConnection, new byte[1], 0, 1);
            });
            await Assert.ThrowsAsync<ConnectionReleasedException>(() =>
            {
                return instance.WriteBytes(establishedConnection, new byte[1], 0, 1);
            });

            // test that repeated call doesn't have effect.
            await instance.ReleaseConnection(establishedConnection);
        }

        [Fact]
        public async Task TestErrorUsage()
        {
            var instance = new MemoryBasedClientTransport();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.AllocateConnection(null));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ReadBytes(new MemoryBasedTransportConnectionInternal(), new byte[1], 0, 0));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.WriteBytes(new MemoryBasedTransportConnectionInternal(), new byte[1], 0, 0));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ReleaseConnection(new MemoryBasedTransportConnectionInternal()));

            instance.Hub = new DefaultMemoryBasedTransportHub();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return instance.ReadBytes(null, new byte[0], 0, 0);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.ReadBytes(4, new byte[2], 0, 1);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.ReadBytes(new MemoryBasedTransportConnectionInternal(), new byte[1], 1, 1);
            });
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
            {
                return instance.WriteBytes(null, new byte[0], 0, 0);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.WriteBytes(4, new byte[2], 0, 1);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.WriteBytes(new MemoryBasedTransportConnectionInternal(), new byte[1], 1, 1);
            });
        }

        [Fact]
        public async Task TestInterleavedOperations()
        {
            var hub = new TestHub();
            var task1 = ForkOperations(hub, "Abidjan");
            var task2 = ForkOperations(hub, "Lagos");
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(task1, task2);
            await Task.WhenAll(task1, task2);
        }

        private async Task ForkOperations(TestHub hub, string localEndpoint)
        {
            var instance = new MemoryBasedClientTransport
            {
                LocalEndpoint = localEndpoint,
                Hub = hub
            };
            hub.ExpectedClient = instance;
            hub.ExpectedConnectivityParams = new DefaultConnectivityParams();
            var connectionResponse = await instance.AllocateConnection(hub.ExpectedConnectivityParams);
            Assert.True(connectionResponse.Connection is MemoryBasedTransportConnectionInternal);

            var establishedConnection = connectionResponse.Connection;

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
            await Assert.ThrowsAsync<ConnectionReleasedException>(() => exTask1);
            await Assert.ThrowsAsync<ConnectionReleasedException>(() => exTask2);
        }

        private async Task ProcessWorkItems(MemoryBasedClientTransport client,
            object connection, bool writeToClient, List<WorkItem> workItems)
        {
            var uncompletedWorkItems = new List<WorkItem>();
            foreach (var pendingWork in workItems)
            {
                if (pendingWork.IsWrite)
                {
                    if (writeToClient)
                    {
                        pendingWork.WriteTask = client.WriteBytes(connection,
                            pendingWork.WriteData, pendingWork.WriteOffset, pendingWork.WriteLength);
                    }
                    else
                    {
                        pendingWork.WriteTask = MemoryBasedServerTransport.WriteBytesInternal(true, connection,
                            pendingWork.WriteData, pendingWork.WriteOffset, pendingWork.WriteLength);
                    }
                }
                else
                {
                    if (writeToClient)
                    {
                        pendingWork.ReadTask = MemoryBasedServerTransport.ReadBytesInternal(true, connection,
                            pendingWork.ReadBuffer, pendingWork.ReadOffset, pendingWork.BytesToRead);
                    }
                    else
                    {
                        pendingWork.ReadTask = client.ReadBytes(connection,
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

        private class TestHub : IMemoryBasedTransportHub
        {
            public IQuasiHttpResponse ProcessSendRequestResult { get; set; }
            public DefaultQuasiHttpRequest ExpectedRequest { get; set; }
            public DefaultConnectivityParams ExpectedConnectivityParams { get;  set; }
            public IQuasiHttpClientTransport ExpectedClient { get; set; }

            public Task AddServer(object endpoint, IQuasiHttpServerTransport server)
            {
                throw new NotImplementedException();
            }

            public Task<IConnectionAllocationResponse> AllocateConnection(IQuasiHttpClientTransport client,
                IConnectivityParams connectivityParams)
            {
                Assert.Equal(ExpectedClient, client);
                Assert.Equal(ExpectedConnectivityParams, connectivityParams);
                var connection = new MemoryBasedTransportConnectionInternal();
                IConnectionAllocationResponse response = new DefaultConnectionAllocationResponse
                {
                    Connection = connection
                };
                return Task.FromResult(response);
            }

            public Task<int> ReadClientBytes(IQuasiHttpClientTransport client, object connection, byte[] data, int offset, int length)
            {
                return MemoryBasedServerTransport.ReadBytesInternal(false, connection, data, offset, length);
            }

            public Task WriteClientBytes(IQuasiHttpClientTransport client, object connection, byte[] data, int offset, int length)
            {
                return MemoryBasedServerTransport.WriteBytesInternal(false, connection, data, offset, length);
            }

            public Task ReleaseClientConnection(IQuasiHttpClientTransport client, object connection)
            {
                return MemoryBasedServerTransport.ReleaseConnectionInternal(connection);
            }
        }
    }
}
