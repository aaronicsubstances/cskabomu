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
        public async Task TestAllocateConnection()
        {
            var instance = new DefaultMemoryBasedTransportHub();
            var server = new MemoryBasedServerTransport
            {
                LocalEndpoint = "gk"
            };
            await instance.AddServer(server);
            await server.Start();
            var connectionAllocationRequest = new DefaultConnectionAllocationRequest
            {
                RemoteEndpoint = server.LocalEndpoint
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
        public async Task TestCanProcessSendRequestDirectly()
        {
            var instance = new DefaultMemoryBasedTransportHub();

            instance.DirectSendRequestProcessingProbability = 0;
            bool canSendDirectly = await instance.CanProcessSendRequestDirectly();
            Assert.False(canSendDirectly);

            instance.DirectSendRequestProcessingProbability = 1;
            canSendDirectly = await instance.CanProcessSendRequestDirectly();
            Assert.True(canSendDirectly);
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
                LocalEndpoint = "gk",
                Application = app
            };
            await instance.AddServer(server);
            await server.Start();
            var connectionAllocationInfo = new DefaultConnectionAllocationRequest
            {
                RemoteEndpoint = server.LocalEndpoint,
                ProcessingMutexApi = expectedMutex
            };
            var response = await instance.ProcessSendRequest("cy", connectionAllocationInfo, expectedReq);
            Assert.Equal(expectedRes, response);
        }
    }
}
