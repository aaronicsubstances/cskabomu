using System;
using System.Net;

namespace Kabomu.Examples.Common
{
    internal class QpcTransaction
    {
        public IPEndPoint RemoteEndpoint { get; set; }
        public int RequestId { get; set; }
        public byte PduType { get; set; }
        public byte[] Data { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }
        public Action<Exception> RequestCallback { get; set; }
        public object RetryBackoffTimeoutId { get; set; }
        public int RetryCount { get; set; }
        public object TimeWaitId { get; internal set; }
    }
}