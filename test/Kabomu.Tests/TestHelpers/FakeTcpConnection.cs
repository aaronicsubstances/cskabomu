using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Tests.TestHelpers
{
    public class FakeTcpConnection
    {
        public FakeTcpConnection()
        {
            DuplexStream = new MemoryStream();
        }

        public object RemoteEndpoint { get; set; }
        public MemoryStream DuplexStream { get; private set; }
        public FakeTcpConnection Peer { get; private set; }
        public bool ConnectionEstablished => Peer != null;

        public void MarkConnectionAsEstablished()
        {
            Peer = new FakeTcpConnection
            {
                Peer = this,
                DuplexStream = new MemoryStream()
            };
            RemoteEndpoint = null;
        }
    }
}
