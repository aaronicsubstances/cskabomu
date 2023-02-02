using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.MemoryBasedTransport
{
    public class DefaultMemoryBasedTransportHubTest
    {
        [Fact]
        public async Task TestForErrors()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultMemoryBasedTransportHub(null));

            var instance = new DefaultMemoryBasedTransportHub();
            var server = new MemoryBasedServerTransport();
            var serverEndpoint = "addr0";
            var validConnectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = serverEndpoint
            };
            var qHttpRequest = new DefaultQuasiHttpRequest();

            // test for errors if server is absent fom hub or found to be null.
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.AllocateConnection(null , validConnectivityParams));

            await instance.AddServer(serverEndpoint, server);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer(serverEndpoint, server));

            // test for argument errors.
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                instance.AddServer("cv", null));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                instance.AddServer(null, new ConfigurableQuasiHttpTransport()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection(null, null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection(null, new DefaultConnectivityParams()));

            // test for errors if server is not started.
            await Assert.ThrowsAnyAsync<Exception>(() =>
                instance.AllocateConnection(null, validConnectivityParams));
        }

        [Fact]
        public async Task TestAllocateConnection()
        {
            var instance = new DefaultMemoryBasedTransportHub();
            var server = new MemoryBasedServerTransport();
            var serverEndpoint = "127.1";
            await instance.AddServer(serverEndpoint, server);
            await server.Start();
            var connectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = serverEndpoint
            };
            var clientConnectTask = instance.AllocateConnection(null, connectivityParams);
            var serverConnectTask = server.ReceiveConnection();
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(clientConnectTask, serverConnectTask);
            await Task.WhenAll(clientConnectTask, serverConnectTask);
            var actualConnectionResponse = await serverConnectTask;
            var expectedConnectionResponse = await clientConnectTask;
            Assert.Equal(expectedConnectionResponse.Connection, actualConnectionResponse.Connection);
        }
    }
}
