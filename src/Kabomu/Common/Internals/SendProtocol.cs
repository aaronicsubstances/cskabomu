using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class SendProtocol
    {
        private readonly ITransferCollection<OutgoingTransfer> _outgoingTransfers =
            new SimpleTransferCollection<OutgoingTransfer>();

        public IQpcFacility QpcService { get; set; } // absolutely required.

        public int DefaultTimeoutMillis { get; set; } // absolutely required.

        public IEventLoopApi EventLoop { get; set; } // absolutely required

        public IRecyclingFactory RecyclingFactory { get; set; }

        public IMessageIdGenerator MessageIdGenerator { get; set; }

        public void BeginSend(IMessageSource msgSource, IMessageTransferOptions options, Action<object, Exception> cb, object cbState)
        {
            var transfer = new OutgoingTransfer
            {
                MessageSource = msgSource,
                MessageSendCallback = cb,
                MessageSendCallbackState = cbState,
                NextPendingResultId = 0
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
                transfer.CancellationHandle = options.CancellationHandle;
                transfer.ReceiveAlreadyStarted = options.StartedAtReceiver;
            }
            if (transfer.MessageId == 0)
            {
                transfer.MessageId = MessageIdGenerator.NextId();
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
            if (!_outgoingTransfers.TryAdd(transfer))
            {
                DisableTransfer(transfer, new Exception("message id in use"));
                return;
            }
            ResetTimeout(transfer);
            transfer.CancellationHandle?.TryAddCancellationListener(_ =>
            {
                EventLoop.PostCallback(_ =>
                    AbortTransfer(transfer, new Exception("cancelled")), null);
            }, null);
            BeginReadMessageSource(transfer);
        }

        private void BeginReadMessageSource(OutgoingTransfer transfer)
        {
            var pendingResultId = transfer.NextPendingResultId;
            MessageSourceCallback cb = (object cbState, Exception error,
                byte[] data, int offset, int length, object alternativePayload, bool hasMore) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.NextPendingResultId == pendingResultId)
                    {
                        transfer.NextPendingResultId++;
                        ProcessSourceResult(transfer, error, data, offset, length, alternativePayload, hasMore);
                    }
                }, null);
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
            transfer.TerminatingChunkSeen = !hasMore;

            SendPendingData(transfer);
        }

        private void SendPendingData(OutgoingTransfer transfer)
        {
            var pendingResultId = transfer.NextPendingResultId;
            Action<object, Exception> cb = (s1, ex) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.NextPendingResultId == pendingResultId)
                    {
                        transfer.NextPendingResultId++;
                        ProcessSendDataOutcome(transfer, ex);
                    }
                }, null);
            };
            byte flags = DefaultProtocolDataUnit.ComputeFlags(transfer.ContinueTransfer,
                !transfer.TerminatingChunkSeen, transfer.ReceiveAlreadyStarted);
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, DefaultProtocolDataUnit.PduTypeData,
                flags, 0, transfer.MessageId, transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingAlternativePayload,
                transfer.CancellationHandle, cb, null);
        }

        public void OnReceiveFirstChunkAck(byte flags, long messageId, byte errorCode)
        {
            throw new NotImplementedException();
        }

        public void OnReceiveSubsequentChunkAck(byte flags, long messageId, byte errorCode)
        {
            throw new NotImplementedException();
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

        public void OnReceiveDataAckPdu(long messageId, byte errorCode)
        {
            if (!_outgoingTransfers.ContainsKey(messageId))
            {
                return;
            }

            var transfer = _outgoingTransfers[messageId];
            if (transfer.AwaitingPendingResult)
            {
                AbortTransfer(transfer, new Exception("stop and wait violation"));
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
            transfer = _outgoingTransfers.TryRemove(transfer);
            if (transfer == null)
            {
                return;
            }
            if (exception == null && (transfer.CancellationHandle?.Cancelled ?? false))
            {
                exception = new Exception("cancelled");
            }
            DisableTransfer(transfer, exception);
        }

        public void ProcessReset(Exception causeOfReset)
        {
            _outgoingTransfers.ForEach(transfer => DisableTransfer(transfer, causeOfReset));
            _outgoingTransfers.Clear();
        }

        private void DisableTransfer(OutgoingTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.ReceiveAckTimeoutId);
            transfer.NextPendingResultId = -1;
            transfer.MessageSource?.OnEndRead(exception);
            transfer.MessageSendCallback?.Invoke(transfer.MessageSendCallbackState, exception);
            transfer.CancellationHandle?.Cancel();
        }
    }
}
