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
        private readonly Dictionary<object, MemoryBasedServerTransport> _servers;

        public DefaultMemoryBasedTransportHub()
        {
            MutexApi = new LockBasedMutexApi();
            _servers = new Dictionary<object, MemoryBasedServerTransport>();
        }

        public IMutexApi MutexApi { get; set; }

        public async Task AddServer(object endpoint, IQuasiHttpServerTransport server)
        {
            if (endpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }
            if (server == null)
            {
                throw new ArgumentException("null server");
            }
            if (server is MemoryBasedServerTransport memoryBased)
            {
                using (await MutexApi.Synchronize())
                {
                    _servers.Add(endpoint, memoryBased);
                }
            }
            else
            {
                throw new ArgumentException("server must be memory based");
            }
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectivityParams connectionAllocationInfo, IQuasiHttpRequest request)
        {
            var serverEndpoint = connectionAllocationInfo?.RemoteEndpoint;
            if (serverEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            MemoryBasedServerTransport server;
            using (await MutexApi.Synchronize())
            {
                if (!_servers.ContainsKey(serverEndpoint))
                {
                    throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
                }
                server = _servers[serverEndpoint];
            }

            var response = await server.ProcessDirectSendRequest(serverEndpoint, clientEndpoint, request);
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

            MemoryBasedServerTransport server;
            using (await MutexApi.Synchronize())
            {
                if (!_servers.ContainsKey(serverEndpoint))
                {
                    throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
                }
                server = _servers[connectivityParams.RemoteEndpoint];
            }

            var connectionAllocationResponse = await server.CreateConnectionForClient(serverEndpoint, clientEndpoint);
            return connectionAllocationResponse;
        }
    }
}
