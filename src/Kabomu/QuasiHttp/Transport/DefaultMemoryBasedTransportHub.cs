using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class DefaultMemoryBasedTransportHub : IMemoryBasedTransportHub
    {
        private readonly Dictionary<object, IQuasiHttpServer> _servers;

        public DefaultMemoryBasedTransportHub()
        {
            MutexApi = new LockBasedMutexApi();
            _servers = new Dictionary<object, IQuasiHttpServer>();
        }

        public IMutexApi MutexApi { get; set; }

        public async Task AddServer(object endpoint, IQuasiHttpServer server)
        {
            if (endpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }
            if (server == null)
            {
                throw new ArgumentException("null server");
            }
            using (await MutexApi.Synchronize())
            {
                _servers.Add(endpoint, server);
            }
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectivityParams connectivityParams, IQuasiHttpRequest request)
        {
            var serverEndpoint = connectivityParams?.RemoteEndpoint;
            if (serverEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            IQuasiHttpApplication destApp;
            using (await MutexApi.Synchronize())
            {
                if (!_servers.ContainsKey(serverEndpoint))
                {
                    throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
                }
                destApp = _servers[serverEndpoint].Application;
            }

            if (destApp == null)
            {
                throw new MissingDependencyException("remote server application");
            }
            var environment = MemoryBasedServerTransport.CreateRequestEnvironment(
                serverEndpoint, clientEndpoint);
            var response = await destApp.ProcessRequest(request, environment);
            return response;
        }

        public async Task<IConnectionAllocationResponse> AllocateConnection(object clientEndpoint,
            IConnectivityParams connectivityParams)
        {
            var serverEndpoint = connectivityParams?.RemoteEndpoint;
            if (serverEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }

            IQuasiHttpServerTransport serverTransport;
            using (await MutexApi.Synchronize())
            {
                if (!_servers.ContainsKey(serverEndpoint))
                {
                    throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
                }
                serverTransport = _servers[connectivityParams.RemoteEndpoint].Transport;
            }

            if (serverTransport == null)
            {
                throw new MissingDependencyException("remote server transport");
            }

            if (serverTransport is MemoryBasedServerTransport memoryBasedServerTransport)
            {
                var connectionAllocationResponse = await memoryBasedServerTransport.CreateConnectionForClient(
                    serverEndpoint, clientEndpoint);
                return connectionAllocationResponse;
            }
            else
            {
                throw new MissingDependencyException("remote server transport is not memory based");
            }
        }
    }
}
