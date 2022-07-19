using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IQuasiHttpClientTransport : IQuasiHttpTransport
    {
        Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams);
    }
}
