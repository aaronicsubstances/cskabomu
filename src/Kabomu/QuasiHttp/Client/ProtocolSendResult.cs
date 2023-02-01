using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Client
{
    public class ProtocolSendResult
    {
        public IQuasiHttpResponse Response { get; set; }

        public bool? ResponseBufferingApplied { get; set; }
    }
}
