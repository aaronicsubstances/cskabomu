using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IQuasiHttpTransportBypass
    {
        Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
               IConnectionAllocationRequest connectionAllocationInfo);
    }
}
