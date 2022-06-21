using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Transports
{
    public class MemoryBasedTransportHub
    {
        public Dictionary<object, MemoryBasedTransport> Transports { get; } = new Dictionary<object, MemoryBasedTransport>();
    }
}
