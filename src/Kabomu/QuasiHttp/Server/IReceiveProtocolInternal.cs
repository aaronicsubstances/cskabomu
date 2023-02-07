using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    interface IReceiveProtocolInternal
    {
        Task Cancel();
        Task<IQuasiHttpResponse> Receive();
    }
}
