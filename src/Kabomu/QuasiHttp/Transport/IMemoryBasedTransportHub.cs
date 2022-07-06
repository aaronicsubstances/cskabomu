using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IMemoryBasedTransportHub
    {
        Task AddServer(MemoryBasedServerTransport server);
        Task<bool> CanProcessSendRequestDirectly();
        Task<IQuasiHttpResponse> ProcessSendRequest(object clientEndpoint,
            IConnectionAllocationRequest connectionAllocationInfo, IQuasiHttpRequest request);
        Task<object> AllocateConnection(object clientEndpoint, 
            IConnectionAllocationRequest connectionRequest);
    }
}
