using Kabomu.QuasiHttp.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IMemoryBasedTransportHub
    {
        /// <summary>
        /// Associates a quasi http server with an endpoint. Separation of endpoint from server enables
        /// multiple endpoints to be associated to a single server.
        /// </summary>
        /// <param name="endpoint">the endpoint associated with this server.</param>
        /// <param name="server">the server associated with the endpoint</param>
        /// <returns>task representing the asynchronous add operation</returns>
        Task AddServer(object endpoint, IQuasiHttpServer server);
        Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectivityParams connectivityParams, IQuasiHttpRequest request);
        Task<IConnectionAllocationResponse> AllocateConnection(object clientEndpoint, 
            IConnectivityParams connectivityParams);
    }
}
