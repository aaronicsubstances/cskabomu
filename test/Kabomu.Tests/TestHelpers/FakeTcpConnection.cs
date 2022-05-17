using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Tests.TestHelpers
{
    public class FakeTcpConnection
    {
        public object RemoteEndpoint { get; set; }
        public MemoryStream DuplexStream { get; set; }
        public bool ConnectionEstablished => RemoteEndpoint == null;
    }
}
