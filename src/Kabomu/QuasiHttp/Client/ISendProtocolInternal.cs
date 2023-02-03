using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    interface ISendProtocolInternal
    {
        Task Cancel();
        Task<ProtocolSendResult> Send();
    }
}
