using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Examples.Shared
{
    internal class LocalhostUdpConnection
    {
        public int PeerPort { get; set; }
        public string ConnectionId { get; set; }
        public int ConnectionRetryCount { get; set; }
        public object ConnectionRetryBackoffId { get; set; }
        public Action<Exception, object> AllocateConnectionCallback { get; set; }

        public override bool Equals(object obj)
        {
            return obj is LocalhostUdpConnection connection &&
                   PeerPort == connection.PeerPort &&
                   ConnectionId == connection.ConnectionId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PeerPort, ConnectionId);
        }
    }
}
