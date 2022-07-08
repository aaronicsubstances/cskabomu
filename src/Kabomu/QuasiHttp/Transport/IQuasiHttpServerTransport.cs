using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IQuasiHttpServerTransport : IQuasiHttpTransport
    {
        IMutexApi MutexApi { get; set; }
        Task Start();
        Task Stop();
        Task<bool> IsRunning();
        Task<IConnectionAllocationResponse> ReceiveConnection();
    }
}
