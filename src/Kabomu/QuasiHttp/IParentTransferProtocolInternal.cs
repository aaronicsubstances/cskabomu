using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal interface IParentTransferProtocolInternal
    {
        Task AbortTransfer(ITransferProtocolInternal transfer);
    }
}
