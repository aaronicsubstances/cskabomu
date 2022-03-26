using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class IncomingTransfer
    {
        public long MessageId { get; set; }
        public bool AwaitingPendingResult { get; set; }
        public int TimeoutMillis { get; set; }
        public ICancellationHandle CancellationHandle { get; set; }
        public IMessageSink MessageSink { get; set; }
        public Action<object, Exception> MessageReceiveCallback { get; set; }
        public object MessageReceiveCallbackState { get; set; }
        public object ReceiveDataTimeoutId { get; set; }
        public int NextPendingResultId { get; set; }
        public bool OpeningChunkSeen { get; set; }
        public bool TerminatingChunkSeen { get; set; }
        public byte[] PendingData { get; set; }
        public int PendingDataOffset { get; set; }
        public int PendingDataLength { get; set; }
        public object PendingAlternativePayload { get; set; }
    }
}
