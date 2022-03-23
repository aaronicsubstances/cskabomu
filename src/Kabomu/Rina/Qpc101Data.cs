using Kabomu.Common.Abstractions;
using System;

namespace Kabomu.Rina
{
    internal class Qpc101Data
    {
        public INetworkAddress RemoteAddress { get; set; }
        public IByteQueue Message { get; set; }
        public Action<object, Exception, IByteQueue> SendCallback { get; set; }
        public object SendCallbackState { get; set; }
        public int RequestTimeoutMillis { get; set; }
        public ICancellationHandle RequestCancellationHandle { get; set; }
        public bool AcknowledgeReceiptBeforeRemoteProcessing { get; set; }
        public int RequestId { get; set; }
        public object RetryBackoffTimeoutId { get; set; }
        public object ReceiveAckTimeoutId { get; set; }
        public ICancellationHandle SendResultCancellationHandle { get; set; }
        public Exception LastSendError { get; set; }
    }
}