using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IDirectSendResult
    {
        IQuasiHttpResponse Response { get; }
        bool? ResponseBufferingApplied { get; }
    }
}
