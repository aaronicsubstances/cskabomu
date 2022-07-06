using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class DefaultMemoryBasedTransportHub : IMemoryBasedTransportHub
    {
        public DefaultMemoryBasedTransportHub()
        {
            MutexApi = new LockBasedMutexApi();
            Servers = new Dictionary<object, MemoryBasedServerTransport>();
        }

        public IMutexApi MutexApi { get; set; }
        public Dictionary<object, MemoryBasedServerTransport> Servers { get; set; }

        public async Task AddServer(string endpoint, MemoryBasedServerTransport server)
        {
            using (await MutexApi.Synchronize())
            {
                Servers.Add(endpoint, server);
            }
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            if (connectionAllocationInfo?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            MemoryBasedServerTransport remoteTransport;
            using (await MutexApi.Synchronize())
            {
                remoteTransport = Servers[connectionAllocationInfo.RemoteEndpoint];
            }

            // ensure remote transport is running, so as to have comparable behaviour
            // with allocate connection
            if (!await remoteTransport.IsRunning())
            {
                throw new Exception("remote transport not started");
            }
            var remoteApp = remoteTransport.Application;
            if (remoteApp == null)
            {
                throw new Exception("remote application not set");
            }
            // can later pass local and remote endpoint information in request environment.
            var response = await remoteApp.ProcessRequest(request, connectionAllocationInfo.Environment);
            return response;
        }

        public async Task<object> AllocateConnection(MemoryBasedClientTransport client,
            IConnectionAllocationRequest connectionRequest)
        {
            if (connectionRequest?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            MemoryBasedServerTransport remoteTransport;
            using (await MutexApi.Synchronize())
            {
                remoteTransport = Servers[connectionRequest.RemoteEndpoint];
            }

            var connection = await remoteTransport.CreateConnectionForClient(connectionRequest, client.LocalEndpoint, client.MutexApi);
            return connection;
        }
    }
}
