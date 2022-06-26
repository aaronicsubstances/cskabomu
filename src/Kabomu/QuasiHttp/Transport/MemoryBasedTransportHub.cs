using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedTransportHub
    {
        public Dictionary<object, MemoryBasedServerTransport> Servers { get; } = new Dictionary<object, MemoryBasedServerTransport>();
    }
}
