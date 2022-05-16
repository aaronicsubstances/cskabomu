using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Examples.Common
{
    internal class LocalhostUdpConnection
    {
        public int DestinationPort { get; set; }
        public string ConnectionId { get; set; }
        public object ConnectionTimeoutId { get; set; }
        public int ConnectionRetryCount { get; set; }
        public object ConnectionRetryBackoffId { get; set; }
        public Action<Exception, object> AllocateConnectionCallback { get; set; }
        public bool Established { get; set; }
        public object ReadTimeoutId { get; set; }
        public long LastReadTime { get; set; }
    }
}
