using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class OutgoingTransfer
    {
        public long MessageId { get; set; }
        public int TimeoutMillis { get; set; }
        public ICancellationHandle CancellationHandle { get; set; }
        public bool ReceiveAlreadyStarted { get; set; }
        public IMessageSource MessageSource { get; set; }
        public Action<object, Exception> MessageSendCallback { get; set; }
        public object MessageSendCallbackState { get; set; }
        public object ReceiveAckTimeoutId { get; set; }
        public int NextPendingResultId { get; set; }
        public bool TerminatingChunkSeen { get; set; }
        public bool ContinueTransfer { get; set; }
        public bool AwaitingPendingResult { get; set; }
        public byte[] PendingData { get; set; }
        public int PendingDataOffset { get; set; }
        public int PendingDataLength { get; set; }
        public object PendingAlternativePayload { get; set; }
    }
}
