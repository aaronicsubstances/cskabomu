using System.Collections.Generic;

namespace Kabomu.Tests.TestHelpers
{
    public class FakeUdpTransportHub
    {
        public Dictionary<object, FakeUdpTransport> Connections { get; } = new Dictionary<object, FakeUdpTransport>();
    }
}