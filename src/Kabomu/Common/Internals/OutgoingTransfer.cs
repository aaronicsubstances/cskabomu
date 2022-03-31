using Kabomu.Common.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class OutgoingTransfer : IRecyclable
    {
        public ITransferEndpoint RemoteEndpoint { get; set; }
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
        public int RecyclingFlags { get; set; }

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
            if (RemoteEndpoint != null && transfer.RemoteEndpoint != null)
            {
                if (RemoteEndpoint.Id != transfer.RemoteEndpoint.Id)
                {
                    return false;
                }
                if (RemoteEndpoint.Name != transfer.RemoteEndpoint.Name)
                {
                    return false;
                }
            }
            else if (RemoteEndpoint != null || transfer.RemoteEndpoint != null)
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
            // Don't include endpoint in hash code computation
            // in order to gain some efficiency by leveraging fact
            // that message id alone almost always uniquely identify a transfer.
            return hashCode;
        }
    }
}
