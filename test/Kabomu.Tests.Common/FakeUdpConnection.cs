using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Common
{
    public class FakeUdpConnection
    {
        public object RemoteEndpoint { get; set; }
        public FakeUdpConnection Peer { get; set; }
    }
}
