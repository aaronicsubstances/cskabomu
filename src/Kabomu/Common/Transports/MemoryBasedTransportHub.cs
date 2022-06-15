using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Transports
{
    public class MemoryBasedTransportHub
    {
        public Dictionary<object, IQuasiHttpClient> Clients { get; } = new Dictionary<object, IQuasiHttpClient>();
    }
}
