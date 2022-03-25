using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class SendProtocol
    {
        private readonly Dictionary<long, OutgoingTransfer> _outgoingTransfers;

        public SendProtocol()
        {
            _outgoingTransfers = new Dictionary<long, OutgoingTransfer>();
        }

        public IQpcFacility QpcService { get; set; } // absolutely required.

        public int DefaultTimeoutMillis { get; set; } // absolutely required.

        public IEventLoopApi EventLoop { get; set; } // absolutely required

        public IRecyclingFactory RecyclingFactory { get; set; }

        public IRandomNumberGenerator RandomNumberGenerator { get; set; }

        public void BeginSend(IMessageSource msgSource, IMessageTransferOptions options, Action<object, Exception> cb, object cbState)
        {
            var transfer = new OutgoingTransfer
            {
                MessageSource = msgSource,
                MessageSendCallback = cb,
                MessageSendCallbackState = cbState
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
                transfer.CancellationHandle = options.CancellationHandle;
                transfer.ContinueTransfer = options.SendToExistingSink;
            }
            if (transfer.MessageId == 0)
            {
                var messageId = RandomNumberGenerator.NextId();
                transfer.MessageId = messageId;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }
            EventLoop.PostCallback(_ =>
            {
                ProcessSend(transfer);
            }, null);
        }

        private void ProcessSend(OutgoingTransfer transfer)
        {
            _outgoingTransfers.Add(transfer.MessageId, transfer);
            ResetTimeout(transfer);
            BeginReadMessageSource(transfer);
        }

        private void BeginReadMessageSource(OutgoingTransfer transfer)
        {
            var pendingResultCancellationHandle = new SimpleCancellationHandle();
            transfer.PendingResultCancellationHandle = pendingResultCancellationHandle;
            MessageSourceCallback cb = (object cbState, Exception error,
                byte[] data, int offset, int length, object alternativePayload, bool hasMore) =>
            {
                if (!pendingResultCancellationHandle.Cancelled)
                {
                    pendingResultCancellationHandle.Cancel();
                    EventLoop.PostCallback(_ => 
                        ProcessSourceResult(transfer, error, data, offset, length, alternativePayload, hasMore), null);
                }
            };
            transfer.AwaitingPendingResult = true;
            transfer.MessageSource.OnDataRead(cb, null);
        }

        private void ProcessSourceResult(OutgoingTransfer transfer, Exception error,
            byte[] data, int offset, int length, object alternativePayload, bool hasMore)
        {
            transfer.AwaitingPendingResult = false;
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }

            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingAlternativePayload = alternativePayload;
            transfer.TerminatingChunkSeen = hasMore;

            SendPendingData(transfer);
        }

        private void SendPendingData(OutgoingTransfer transfer)
        {
            var cancellationHandle = new SimpleCancellationHandle();
            transfer.PendingResultCancellationHandle = cancellationHandle;
            Action<object, Exception> cb = (s1, ex) =>
            {
                if (!cancellationHandle.Cancelled)
                {
                    cancellationHandle.Cancel();
                    EventLoop.PostCallback(s2 => ProcessSendDataOutcome(
                        transfer, ex), null);
                }
            };
            byte flags = 0;
            if (transfer.ContinueTransfer)
            {
                flags |= 1 << 0;
            }
            if (transfer.TerminatingChunkSeen)
            {
                flags |= 1 << 1;
            }
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, DefaultProtocolDataUnit.PduTypeData,
                flags, 0, transfer.MessageId, transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingAlternativePayload,
                transfer.CancellationHandle, cb, null);
        }

        private void ProcessSendDataOutcome(OutgoingTransfer transfer, Exception ex)
        {
            transfer.AwaitingPendingResult = false;
            if (ex != null)
            {
                AbortTransfer(transfer, ex);
                return;
            }
        }

        public void OnReceiveDataAckPdu(byte flags, byte errorCode, long messageId)
        {
            if (!_outgoingTransfers.ContainsKey(messageId))
            {
                return;
            }

            var transfer = _outgoingTransfers[messageId];
            if (transfer.AwaitingPendingResult)
            {
                return;
            }

            if (errorCode != 0)
            {
                AbortTransfer(transfer, new Exception("remote peer error: " + errorCode));
                return;
            }

            // advance transfer.
            if (transfer.TerminatingChunkSeen)
            {
                AbortTransfer(transfer, null);
                return;
            }

            ResetTimeout(transfer);
            transfer.ContinueTransfer = true;
            BeginReadMessageSource(transfer);
        }

        private void ResetTimeout(OutgoingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.ReceiveAckTimeoutId);
            transfer.ReceiveAckTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("send timeout"));
                }, null);
        }

        private void AbortTransfer(OutgoingTransfer transfer, Exception exception)
        {
            _outgoingTransfers.Remove(transfer.MessageId);
            DisableTransfer(transfer, exception);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            foreach (var transfer in _outgoingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _outgoingTransfers.Clear();
        }

        private void DisableTransfer(OutgoingTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.ReceiveAckTimeoutId);
            transfer.PendingResultCancellationHandle?.Cancel();
            transfer.MessageSource.OnEndRead(exception);
            transfer.MessageSendCallback?.Invoke(transfer.MessageSendCallbackState, exception);
            transfer.MessageSource = null;
            transfer.CancellationHandle = null;
            transfer.PendingData = null;
            transfer.PendingAlternativePayload = null;
        }
    }
}
