using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IQuasiHttpServerTransport : IQuasiHttpTransport
    {
        bool IsRunning { get; }
        Task Start();
        Task Stop();
        Task<IConnectionAllocationResponse> ReceiveConnection();
    }
}
