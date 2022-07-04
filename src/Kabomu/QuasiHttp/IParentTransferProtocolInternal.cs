using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal interface IParentTransferProtocolInternal
    {
        IQuasiHttpApplication Application { get; }
        IQuasiHttpTransport Transport { get; }
        Task AbortTransfer(ITransferProtocolInternal transfer);
    }
}
