using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Client
{
    internal class ProtocolSendResultInternal
    {
        public IQuasiHttpResponse Response { get; set; }

        public bool? ResponseBufferingApplied { get; set; }
    }
}
