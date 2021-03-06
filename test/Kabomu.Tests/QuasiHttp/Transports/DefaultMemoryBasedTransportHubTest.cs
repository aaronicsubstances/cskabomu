using Kabomu.Common;
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
            var server = new ConfigurableQuasiHttpServer();
            var serverEndpoint = "addr0";
            var validConnectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = serverEndpoint
            };
            var qHttpRequest = new DefaultQuasiHttpRequest();

            // test for errors if server is absent fom hub.
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.AllocateConnection("kl", validConnectivityParams));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest("kl", validConnectivityParams, qHttpRequest));

            await instance.AddServer(serverEndpoint, server);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer(serverEndpoint, server));

            // test for argument errors.
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer("cv", null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer(null, new ConfigurableQuasiHttpServer()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection("kl", null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection("kl", new DefaultConnectivityParams()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest("kl", null, qHttpRequest));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest("kl", new DefaultConnectivityParams(), qHttpRequest));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest("kl", validConnectivityParams, null));

            // test for errors if server transport is not provided or not memory based.
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest("kl", validConnectivityParams, qHttpRequest));
            server.Transport = new ConfigurableQuasiHttpTransport();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest("kl", validConnectivityParams, qHttpRequest));

            // test for errors if server transport is valid but is not started.
            var serverTransport = new MemoryBasedServerTransport();
            server.Transport = serverTransport;
            await Assert.ThrowsAnyAsync<Exception>(() =>
                instance.AllocateConnection("kl", validConnectivityParams));

            // test for errors if application is not set after server transport is started.
            await serverTransport.Start();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest("te", validConnectivityParams, qHttpRequest));
        }

        [Fact]
        public async Task TestAllocateConnection()
        {
            var instance = new DefaultMemoryBasedTransportHub();
            var serverTransport = new MemoryBasedServerTransport();
            var serverEndpoint = "127.1";
            await instance.AddServer(serverEndpoint, new ConfigurableQuasiHttpServer
            {
                Transport = serverTransport
            });
            await serverTransport.Start();
            var connectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = serverEndpoint
            };
            var clientConnectTask = instance.AllocateConnection(null, connectivityParams);
            var serverConnectTask = serverTransport.ReceiveConnection();
            // use whenany before whenall to catch any task exceptions which may
            // cause another task to hang forever.
            await await Task.WhenAny(clientConnectTask, serverConnectTask);
            await Task.WhenAll(clientConnectTask, serverConnectTask);
            var actualConnectionResponse = await serverConnectTask;
            var expectedConnectionResponse = await clientConnectTask;
            Assert.Equal(expectedConnectionResponse.Connection, actualConnectionResponse.Connection);
        }

        [Fact]
        public async Task TestProcessSendRequest()
        {
            var instance = new DefaultMemoryBasedTransportHub();
            var expectedReq = new DefaultQuasiHttpRequest();
            var expectedRes = new DefaultQuasiHttpResponse();
            var app = new ConfigurableQuasiHttpApplication
            {
                ProcessRequestCallback = (req, env) =>
                {
                    Assert.Equal(expectedReq, req);
                    Assert.Equal(new Dictionary<string, object>(), env);
                    return Task.FromResult<IQuasiHttpResponse>(expectedRes);
                }
            };
            var serverTransport = new MemoryBasedServerTransport();
            var serverEndpoint = "dea";
            await instance.AddServer(serverEndpoint, new ConfigurableQuasiHttpServer
            {
                Transport = serverTransport,
                Application = app
            }); ;
            var connectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = serverEndpoint
            };
            var response = await instance.ProcessSendRequest("cy", connectivityParams, expectedReq);
            Assert.Equal(expectedRes, response);
        }
    }
}
