using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IMemoryBasedTransportHub
    {
        /// <summary>
        /// Separation of endpoint from server enables multiple endpoints to be associated to
        /// a single server.
        /// </summary>
        /// <param name="endpoint">the endpoint associated with this server.</param>
        /// <param name="server"></param>
        /// <returns></returns>
        Task AddServer(object endpoint, IQuasiHttpServerTransport server);
        Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectionAllocationRequest connectionAllocationInfo, IQuasiHttpRequest request);
        Task<object> AllocateConnection(object clientEndpoint, 
            IConnectionAllocationRequest connectionRequest);
    }
}
