using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    interface IReceiveProtocolInternal
    {
        void Cancel();
        Task<IQuasiHttpResponse> Receive();
    }
}
