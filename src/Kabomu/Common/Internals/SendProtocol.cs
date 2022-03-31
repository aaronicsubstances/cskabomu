﻿using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class SendProtocol
    {
        private readonly ITransferCollection<OutgoingTransfer> _outgoingTransfers =
            new DefaultTransferCollection<OutgoingTransfer>();

        public IQpcFacility QpcService { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public IMessageIdGenerator MessageIdGenerator { get; set; }

        public long BeginSend(object connectionHandle, IMessageSource msgSource, IMessageTransferOptions options,
            Action<object, Exception> cb, object cbState)
        {
            long messageId = MessageIdGenerator.NextId();
            var transfer = new OutgoingTransfer
            {
                MessageId = messageId,
                MessageSource = msgSource,
                MessageSendCallback = cb,
                MessageSendCallbackState = cbState,
                RequestConnectionHandle = connectionHandle
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
                transfer.CancellationIndicator = options.CancellationIndicator;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }
            EventLoop.PostCallback(_ =>
            {
                ProcessSend(transfer);
            }, null);
            return messageId;
        }

        public void BeginSendStartedAtReceiver(object connectionHandle, IMessageSource msgSource, long msgIdAtReceiver,
            IMessageTransferOptions options, Action<object, Exception> cb, object cbState)
        {
            var transfer = new OutgoingTransfer
            {
                MessageId = msgIdAtReceiver,
                StartedAtReceiver = true,
                MessageSource = msgSource,
                MessageSendCallback = cb,
                MessageSendCallbackState = cbState,
                RequestConnectionHandle = connectionHandle
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
                transfer.CancellationIndicator = options.CancellationIndicator;
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
            if (transfer.StartedAtReceiver && _outgoingTransfers.TryGet(transfer) != null)
            {
                // Intepret as a valid attempt to reuse a message id for a new transfer started by receiver.
                // Abort existing transfer and create a new one to replace it.
                AbortTransfer(transfer, new Exception("aborted by receiver"));
            }
            if (!_outgoingTransfers.TryAdd(transfer))
            {
                DisableTransfer(transfer, new Exception("message id in use"));
                return;
            }
            ResetTimeout(transfer);
            BeginReadMessageSource(transfer);
        }

        private void BeginReadMessageSource(OutgoingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.PendingResultCancellationIndicator = cancellationIndicator;
            MessageSourceCallback cb = (object cbState, Exception error,
                byte[] data, int offset, int length, object fallbackPayload, bool hasMore) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.PendingResultCancellationIndicator == cancellationIndicator)
                    {
                        transfer.PendingResultCancellationIndicator = null;
                        ProcessSourceResult(transfer, error, data, offset, length, fallbackPayload, hasMore);
                    }
                }, null);
            };
            transfer.MessageSource.OnDataRead(cb, null);
        }

        private void ProcessSourceResult(OutgoingTransfer transfer, Exception error,
            byte[] data, int offset, int length, object fallbackPayload, bool hasMore)
        {
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }

            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingFallbackPayload = fallbackPayload;
            transfer.TerminatingChunkSeen = !hasMore;

            SendPendingData(transfer);
        }

        private void SendPendingData(OutgoingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.PendingResultCancellationIndicator = cancellationIndicator;
            Action<object, Exception> cb = (s1, ex) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.PendingResultCancellationIndicator == cancellationIndicator)
                    {
                        transfer.PendingResultCancellationIndicator = null;
                        ProcessSendDataOutcome(transfer, ex);
                    }
                }, null);
            };
            byte flags = DefaultProtocolDataUnit.ComputeFlags(transfer.StartedAtReceiver,
                !transfer.TerminatingChunkSeen);
            byte pduType = transfer.OpeningChunkSent ? DefaultProtocolDataUnit.PduTypeSubsequentChunk :
                DefaultProtocolDataUnit.PduTypeFirstChunk;
            object connectionHandle = transfer.RequestConnectionHandle;
            transfer.RequestConnectionHandle = null;
            QpcService.BeginSendPdu(connectionHandle, DefaultProtocolDataUnit.Version01, pduType,
                flags, 0, transfer.MessageId, transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingFallbackPayload,
                transfer.CancellationIndicator, cb, null);
        }

        private void ProcessSendDataOutcome(OutgoingTransfer transfer, Exception ex)
        {
            if (ex != null)
            {
                AbortTransfer(transfer, ex);
                return;
            }
        }

        public void OnReceiveFirstChunkAck(object connectionHandle, byte flags, long messageId, byte errorCode)
        {
            bool startedAtReceiver = DefaultProtocolDataUnit.IsStartedAtReceiverFlagPresent(flags);
            var transfer = _outgoingTransfers.TryGet(new OutgoingTransfer
            {
                MessageId = messageId,
                StartedAtReceiver = startedAtReceiver
            });
            if (transfer != null)
            {
                return;
            }
            transfer.RequestConnectionHandle = connectionHandle;
            if (transfer.PendingResultCancellationIndicator != null)
            {
                AbortTransfer(transfer, new Exception("stop and wait violation"));
                return;
            }
            if (transfer.OpeningChunkSent)
            {
                AbortTransfer(transfer, new Exception("expected subsequent ack"));
                return;
            }
            if (errorCode != 0)
            {
                AbortTransfer(transfer, new Exception("remote peer error: " + errorCode));
                return;
            }

            transfer.OpeningChunkSent = true;
            AdvanceTransfer(transfer);
        }

        public void OnReceiveSubsequentChunkAck(object connectionHandle, byte flags, long messageId, byte errorCode)
        {
            bool startedAtReceiver = DefaultProtocolDataUnit.IsStartedAtReceiverFlagPresent(flags);
            var transfer = _outgoingTransfers.TryGet(new OutgoingTransfer
            {
                MessageId = messageId,
                StartedAtReceiver = startedAtReceiver
            });
            if (transfer != null)
            {
                return;
            }
            transfer.RequestConnectionHandle = connectionHandle;
            if (transfer.PendingResultCancellationIndicator != null)
            {
                AbortTransfer(transfer, new Exception("stop and wait violation"));
                return;
            }
            if (!transfer.OpeningChunkSent)
            {
                AbortTransfer(transfer, new Exception("expected first ack"));
                return;
            }
            if (errorCode != 0)
            {
                AbortTransfer(transfer, new Exception("remote peer error: " + errorCode));
                return;
            }

            AdvanceTransfer(transfer);
        }


        private void AdvanceTransfer(OutgoingTransfer transfer)
        {
            if (transfer.TerminatingChunkSeen)
            {
                AbortTransfer(transfer, null);
                return;
            }

            ResetTimeout(transfer);
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
            if (exception == null && (transfer.CancellationIndicator?.Cancelled ?? false))
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
            transfer.ReceiveAckTimeoutId = null;
            transfer.PendingResultCancellationIndicator?.Cancel();
            transfer.PendingResultCancellationIndicator = null;
            transfer.MessageSource?.OnEndRead(exception);
            transfer.MessageSource = null;
            transfer.MessageSendCallback?.Invoke(transfer.MessageSendCallbackState, exception);
            transfer.MessageSendCallback = null;
            transfer.MessageSendCallbackState = null;
            transfer.CancellationIndicator = null;
            transfer.PendingData = null;
            transfer.PendingFallbackPayload = null;
            transfer.RequestConnectionHandle = null;
        }
    }
}