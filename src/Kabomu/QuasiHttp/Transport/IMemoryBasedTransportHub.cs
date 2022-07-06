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
        Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo);
        Task<object> AllocateConnection(MemoryBasedClientTransport client, 
            IConnectionAllocationRequest connectionRequest);
    }
}
