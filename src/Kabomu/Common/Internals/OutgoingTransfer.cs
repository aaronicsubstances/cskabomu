using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class OutgoingTransfer
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
        public object PendingFallbackPayload { get; set; }
        public object RequestConnectionHandle { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is OutgoingTransfer))
            {
                return false;
            }
            var transfer = (OutgoingTransfer)obj;
            if (MessageId != transfer.MessageId)
            {
                return false;
            }
            if (StartedAtReceiver != transfer.StartedAtReceiver)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hashCode = 273286397;
            // Avoid method call to long.GetHashCode() by
            // using java's example of computing hash code of a long value as
            // = (int)(longValue ^ (longValue >>> 32))
            hashCode = hashCode * -1521134295 + (int)(MessageId ^ (MessageId >> 32));
            hashCode = hashCode * -1521134295 + (StartedAtReceiver ? 1 : 0);
            return hashCode;
        }
    }
}
