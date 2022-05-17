using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.TestHelpers
{
    public class FakeUdpConnection
    {
        public object RemoteEndpoint { get; set; }
        public FakeUdpConnection Peer { get; set; }
        public bool ConnectionEstablished => Peer != null;
    }
}
