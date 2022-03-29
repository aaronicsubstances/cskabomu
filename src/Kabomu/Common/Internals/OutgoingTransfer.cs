using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class OutgoingTransfer : IRecyclable
    {
        public long MessageId { get; set; }
        public bool StartedAtReceiver { get; set; }
        public int TimeoutMillis { get; set; }
        public ICancellationIndicator CancellationIndicator { get; set; }
        public IMessageSource MessageSource { get; set; }
        public Action<object, Exception> MessageSendCallback { get; set; }
        public object MessageSendCallbackState { get; set; }
        public object ReceiveAckTimeoutId { get; set; }
        public STCancellationIndicator PendingResultCancellationIndicator { get; set; }
        public bool OpeningChunkSent { get; set; }
        public bool TerminatingChunkSeen { get; set; }
        public byte[] PendingData { get; set; }
        public int PendingDataOffset { get; set; }
        public int PendingDataLength { get; set; }
        public object PendingAlternativePayload { get; set; }
        public int RecyclingFlags { get; set; }
    }
}
