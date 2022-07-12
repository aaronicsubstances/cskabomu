﻿using Kabomu.Common;
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
            IConnectionAllocationRequest connectionAllocationInfo, IQuasiHttpRequest request)
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

            var response = await server.ProcessDirectSendRequest(serverEndpoint, clientEndpoint, 
                connectionAllocationInfo.ProcessingMutexApi, request);
            return response;
        }

        public async Task<object> AllocateConnection(object clientEndpoint,
            IConnectionAllocationRequest connectionRequest)
        {
            var serverEndpoint = connectionRequest?.RemoteEndpoint;
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
                server = _servers[connectionRequest.RemoteEndpoint];
            }

            var connection = await server.CreateConnectionForClient(serverEndpoint, clientEndpoint,
                connectionRequest.ProcessingMutexApi);
            return connection;
        }
    }
}
