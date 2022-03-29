using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class IncomingTransfer : IRecyclable
    {
        public long MessageId { get; set; }
        public bool StartedAtReceiver { get; set; }
        public int TimeoutMillis { get; set; }
        public ICancellationIndicator CancellationIndicator { get; set; }
        public IMessageSink MessageSink { get; set; }
        public Action<object, Exception> MessageReceiveCallback { get; set; }
        public object MessageReceiveCallbackState { get; set; }
        public object ReceiveDataTimeoutId { get; set; }
        public STCancellationIndicator PendingResultCancellationIndicator { get; set; }
        public bool OpeningChunkReceived { get; set; }
        public bool TerminatingChunkSeen { get; set; }
        public byte[] PendingData { get; set; }
        public int PendingDataOffset { get; set; }
        public int PendingDataLength { get; set; }
        public object PendingAlternativePayload { get; set; }
        public int RecyclingFlags { get; set; }
    }
}
