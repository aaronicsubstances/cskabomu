using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public class DefaultDirectSendResult : IDirectSendResult
    {
        public IQuasiHttpResponse Response { get; set; }

        public bool? ResponseBufferingApplied { get; set; }
    }
}
