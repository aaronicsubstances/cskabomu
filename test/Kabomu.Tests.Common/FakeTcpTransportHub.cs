using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common
{
    public class FakeTcpTransportHub
    {
        public Dictionary<object, FakeTcpTransport> Connections { get; } = new Dictionary<object, FakeTcpTransport>();
    }
}
