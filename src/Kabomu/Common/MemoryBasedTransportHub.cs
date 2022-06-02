using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class MemoryBasedTransportHub
    {
        public Dictionary<object, KabomuQuasiHttpClient> Connections { get; } = new Dictionary<object, KabomuQuasiHttpClient>();
    }
}
