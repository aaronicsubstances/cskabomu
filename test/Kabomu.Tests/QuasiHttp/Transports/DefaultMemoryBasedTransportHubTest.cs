using Kabomu.Common;
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
    public class DefaultMemoryBasedTransportHubTest
    {
        [Fact]
        public async Task TestForErrors()
        {
            var instance = new DefaultMemoryBasedTransportHub();
            var server = new MemoryBasedServerTransport();
            var serverEndpoint = "addr0";
            var validConnectionRequest = new DefaultConnectionAllocationRequest
            {
                RemoteEndpoint = serverEndpoint
            };
            var qHttpRequest = new DefaultQuasiHttpRequest();

            // test for errors if server is absent fom hub.
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.AllocateConnection("kl", validConnectionRequest));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest("kl", validConnectionRequest, qHttpRequest));

            await instance.AddServer(serverEndpoint, server);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer(serverEndpoint, server));

            // test for argument errors.
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer("cv", null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer(null, new MemoryBasedServerTransport()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection("kl", null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection("kl", new DefaultConnectionAllocationRequest()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest("kl", null, qHttpRequest));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest("kl", new DefaultConnectionAllocationRequest(), qHttpRequest));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest("kl", validConnectionRequest, null));

            // test for errors if server is not started.
            await Assert.ThrowsAnyAsync<Exception>(() =>
                instance.AllocateConnection("kl", validConnectionRequest));
            await Assert.ThrowsAnyAsync<Exception>(() =>
                instance.ProcessSendRequest("kl", validConnectionRequest, qHttpRequest));

            // test for errors if application is not set after server is started.
            await server.Start();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest("te", validConnectionRequest, qHttpRequest));
        }

        [Fact]
        public async Task TestAllocateConnection()
        {
            var instance = new DefaultMemoryBasedTransportHub();
            var server = new MemoryBasedServerTransport();
            var serverEndpoint = "127.1";
            await instance.AddServer(serverEndpoint, server);
            await server.Start();
            var connectionAllocationRequest = new DefaultConnectionAllocationRequest
            {
                RemoteEndpoint = serverEndpoint
            };
            var clientConnectTask = instance.AllocateConnection(null, connectionAllocationRequest);
            var serverConnectTask = server.ReceiveConnection();
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(clientConnectTask, serverConnectTask);
            await Task.WhenAll(clientConnectTask, serverConnectTask);
            var actualConnectionResponse = await serverConnectTask;
            var expectedConnection = await clientConnectTask;
            Assert.Equal(expectedConnection, actualConnectionResponse.Connection);
        }

        [Fact]
        public async Task TestProcessSendRequest()
        {
            var instance = new DefaultMemoryBasedTransportHub();
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
            var server = new MemoryBasedServerTransport
            {
                Application = app
            };
            var serverEndpoint = "dea";
            await instance.AddServer(serverEndpoint, server);
            await server.Start();
            var connectionAllocationInfo = new DefaultConnectionAllocationRequest
            {
                RemoteEndpoint = serverEndpoint,
                ProcessingMutexApi = expectedMutex
            };
            var response = await instance.ProcessSendRequest("cy", connectionAllocationInfo, expectedReq);
            Assert.Equal(expectedRes, response);
        }
    }
}
