using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class DefaultMemoryBasedTransportHub : IMemoryBasedTransportHub
    {
        private readonly Random _randGen = new Random();

        public DefaultMemoryBasedTransportHub()
        {
            MutexApi = new LockBasedMutexApi();
            Servers = new Dictionary<object, MemoryBasedServerTransport>();
        }

        public double DirectSendRequestProcessingProbability { get; set; }
        public IMutexApi MutexApi { get; set; }
        public Dictionary<object, MemoryBasedServerTransport> Servers { get; set; }

        public async Task AddServer(MemoryBasedServerTransport server)
        {
            if (server == null)
            {
                throw new ArgumentException("null server");
            }
            if (server.LocalEndpoint == null)
            {
                throw new ArgumentException("null endpoint");
            }
            using (await MutexApi.Synchronize())
            {
                Servers.Add(server.LocalEndpoint, server);
            }
        }

        public async Task<bool> CanProcessSendRequestDirectly()
        {
            using (await MutexApi.Synchronize())
            {
                return _randGen.NextDouble() < DirectSendRequestProcessingProbability;
            }
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectionAllocationRequest connectionAllocationInfo, IQuasiHttpRequest request)
        {
            if (connectionAllocationInfo?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            MemoryBasedServerTransport server;
            using (await MutexApi.Synchronize())
            {
                server = Servers[connectionAllocationInfo.RemoteEndpoint];
            }

            var response = await server.ProcessDirectSendRequest(clientEndpoint, 
                connectionAllocationInfo.ProcessingMutexApi, request);
            return response;
        }

        public async Task<object> AllocateConnection(object clientEndpoint,
            IConnectionAllocationRequest connectionRequest)
        {
            if (connectionRequest?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            MemoryBasedServerTransport server;
            using (await MutexApi.Synchronize())
            {
                server = Servers[connectionRequest.RemoteEndpoint];
            }

            var connection = await server.CreateConnectionForClient(clientEndpoint,
                connectionRequest.ProcessingMutexApi);
            return connection;
        }
    }
}
