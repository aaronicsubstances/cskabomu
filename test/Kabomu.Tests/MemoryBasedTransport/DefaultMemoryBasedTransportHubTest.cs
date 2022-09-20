using Kabomu.Common;
using Kabomu.MemoryBasedTransport;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
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
            var server = new ConfigurableQuasiHttpServer();
            var serverEndpoint = "addr0";
            var validConnectivityParams = new DefaultConnectivityParams
            {
                RemoteEndpoint = serverEndpoint
            };
            var qHttpRequest = new DefaultQuasiHttpRequest();

            // test for errors if server is absent fom hub or found to be null.
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.AllocateConnection(null , validConnectivityParams));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest(null, validConnectivityParams, qHttpRequest));
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
            {
                var externalServers = new Dictionary<object, IQuasiHttpServer>
                {
                    { serverEndpoint, null }
                };
                var instanceWithExternalServers = new DefaultMemoryBasedTransportHub(externalServers);
                return instanceWithExternalServers.ProcessSendRequest(null, validConnectivityParams, qHttpRequest);
            });

            await instance.AddServer(serverEndpoint, server);
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AddServer(serverEndpoint, server));

            // test for argument errors.
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                instance.AddServer("cv", null));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                instance.AddServer(null, new ConfigurableQuasiHttpServer()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection(null, null));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.AllocateConnection(null, new DefaultConnectivityParams()));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest(null, null, qHttpRequest));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ProcessSendRequest(null, new DefaultConnectivityParams(), qHttpRequest));
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                instance.ProcessSendRequest(null, validConnectivityParams, null));

            // test for errors if server transport is not provided or not memory based.
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest(null, validConnectivityParams, qHttpRequest));
            server.Transport = new ConfigurableQuasiHttpTransport();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest(null, validConnectivityParams, qHttpRequest));

            // test for errors if server transport is valid but is not started.
            var serverTransport = new MemoryBasedServerTransport();
            server.Transport = serverTransport;
            await Assert.ThrowsAnyAsync<Exception>(() =>
                instance.AllocateConnection(null, validConnectivityParams));

            // test for errors if application is not set after server transport is started.
            await serverTransport.Start();
            await Assert.ThrowsAsync<MissingDependencyException>(() =>
                instance.ProcessSendRequest(null, validConnectivityParams, qHttpRequest));
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
                    var expectedEnv = new Dictionary<string, object>
                    {
                        { TransportUtils.ReqEnvKeyLocalPeerEndpoint, "dea" },
                        { TransportUtils.ReqEnvKeyRemotePeerEndpoint, null },
                    };
                    Assert.Equal(expectedEnv, env);
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
            var response = await instance.ProcessSendRequest(null, connectivityParams, expectedReq);
            Assert.Equal(expectedRes, response);
        }
    }
}
