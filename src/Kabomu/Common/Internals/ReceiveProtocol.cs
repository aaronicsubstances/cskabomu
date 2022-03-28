using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class ReceiveProtocol
    {
        private readonly ITransferCollection<IncomingTransfer> _incomingTransfers = 
            new SimpleTransferCollection<IncomingTransfer>();

        public IQpcFacility QpcService { get; set; }
        public IMessageSinkFactory MessageSinkFactory { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public IMessageIdGenerator MessageIdGenerator { get; set; }

        public long BeginReceive(IMessageSink msgSink, IMessageTransferOptions options, Action<object, Exception> cb, object cbState)
        {
            long messageId = MessageIdGenerator.NextId();
            var transfer = new IncomingTransfer
            {
                MessageId = messageId,
                StartedAtReceiver = true,
                MessageSink = msgSink,
                MessageReceiveCallback = cb,
                MessageReceiveCallbackState = cbState
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
                ProcessReceive(transfer);
            }, null);
            return messageId;
        }

        private void ProcessReceive(IncomingTransfer transfer)
        {
            if (!_incomingTransfers.TryAdd(transfer))
            {
                DisableTransfer(transfer, new Exception("message id in use"));
                return;
            }
            ResetTimeout(transfer);
        }

        public void OnReceiveFirstChunk(byte flags, long messageId,
            byte[] data, int offset, int length, object alternativePayload)
        {
            bool startedAtReceiver = DefaultProtocolDataUnit.IsStartedAtReceiverFlagPresent(flags);
            IncomingTransfer transfer = _incomingTransfers.TryGet(new IncomingTransfer
            {
                MessageId = messageId,
                StartedAtReceiver = startedAtReceiver
            });
            if (startedAtReceiver)
            {
                if (transfer == null)
                {
                    SendAck(messageId, DefaultProtocolDataUnit.PduTypeFirstChunkAck, 1);
                    return;
                }
                if (transfer.AwaitingPendingResult)
                {
                    AbortTransfer(transfer, new Exception("stop and wait violation"));
                    SendTransferAck(transfer, 1);
                    return;
                }
                if (transfer.OpeningChunkReceived)
                {
                    // Intepret as an illegal attempt to use a first chunk to continue a transfer.
                    AbortTransfer(transfer, new Exception("protocol violation: first chunk cannot continue a transfer"));
                    SendTransferAck(transfer, 1);
                    return;
                }
            }
            else
            {
                if (transfer != null)
                {
                    // Intepret as a valid attempt to reuse a message id for a new transfer started by sender.
                    // Abort existing transfer and create a new one to replace it.
                    AbortTransfer(transfer, new Exception("aborted by sender"));
                }
                transfer = new IncomingTransfer
                {
                    MessageId = messageId,
                    StartedAtReceiver = false,
                    TimeoutMillis = DefaultTimeoutMillis
                };
                if (!_incomingTransfers.TryAdd(transfer))
                {
                    DisableTransfer(transfer, new Exception("could not add to transfer collection"));
                    return;
                }
                ResetTimeout(transfer);
            }

            bool hasMore = DefaultProtocolDataUnit.IsHasMoreFlagPresent(flags);
            transfer.TerminatingChunkSeen = !hasMore;
            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingAlternativePayload = alternativePayload;
            if (startedAtReceiver)
            {
                BeginWriteMessageSink(transfer);
            }
            else
            {
                BeginCreateMessageSink(transfer);
            }
        }

        public void OnReceiveSubsequentChunk(byte flags, long messageId,
            byte[] data, int offset, int length, object alternativePayload)
        {
            bool startedAtReceiver = DefaultProtocolDataUnit.IsStartedAtReceiverFlagPresent(flags);
            IncomingTransfer transfer = _incomingTransfers.TryGet(new IncomingTransfer
            {
                MessageId = messageId,
                StartedAtReceiver = startedAtReceiver
            });
            if (transfer == null)
            {
                SendAck(messageId, DefaultProtocolDataUnit.PduTypeSubsequentChunkAck, 1);
                return;
            }
            if (transfer.AwaitingPendingResult)
            {
                AbortTransfer(transfer, new Exception("stop and wait violation"));
                SendTransferAck(transfer, 1);
                return;
            }
            if (!transfer.OpeningChunkReceived)
            {
                // Intepret as an illegal attempt to use a subsequent chunk to start a transfer.
                AbortTransfer(transfer, new Exception("protocol violation: subsequent chunk cannot start a transfer"));
                SendTransferAck(transfer, 1);
                return;
            }

            bool hasMore = DefaultProtocolDataUnit.IsHasMoreFlagPresent(flags);
            transfer.TerminatingChunkSeen = !hasMore;
            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingAlternativePayload = alternativePayload;

            BeginWriteMessageSink(transfer);
        }

        private void BeginCreateMessageSink(IncomingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.CancellationIndicator = cancellationIndicator;
            MessageSinkCreationCallback cb = (object cbState, Exception error, IMessageSink sink, 
                ICancellationIndicator externalCancellationIndicator,
                Action<object, Exception> recvCb, object recvCbState) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.PendingResultCancellationIndicator == cancellationIndicator)
                    {
                        transfer.PendingResultCancellationIndicator = null;
                        ProcessSinkCreationResult(transfer, error, sink, externalCancellationIndicator, recvCb, recvCbState);
                    }
                }, null);
            };
            transfer.AwaitingPendingResult = true;
            MessageSinkFactory.CreateMessageSink(cb, null);
        }

        private void ProcessSinkCreationResult(IncomingTransfer transfer, Exception error, 
            IMessageSink sink, ICancellationIndicator cancellationIndicator, Action<object, Exception> recvCb, object recvCbState)
        {
            transfer.AwaitingPendingResult = false;
            if (error != null)
            {
                AbortTransfer(transfer, error);
                SendTransferAck(transfer, 1);
                return;
            }

            transfer.MessageSink = sink;
            transfer.MessageReceiveCallback = recvCb;
            transfer.MessageReceiveCallbackState = recvCbState;
            transfer.CancellationIndicator = cancellationIndicator;

            ResetTimeout(transfer);
            BeginWriteMessageSink(transfer);
        }

        private void BeginWriteMessageSink(IncomingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.CancellationIndicator = cancellationIndicator;
            MessageSinkCallback cb = (object cbState, Exception error) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.PendingResultCancellationIndicator == cancellationIndicator)
                    {
                        transfer.PendingResultCancellationIndicator = null;
                        ProcessSinkResult(transfer, error);
                    }
                }, null);
            };
            transfer.AwaitingPendingResult = true;
            transfer.MessageSink.OnDataWrite(transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingAlternativePayload, !transfer.TerminatingChunkSeen, cb, null);
        }

        private void ProcessSinkResult(IncomingTransfer transfer, Exception error)
        {
            transfer.AwaitingPendingResult = false;
            if (error != null)
            {
                AbortTransfer(transfer, error);
                SendTransferAck(transfer, 1);
                return;
            }

            transfer.OpeningChunkReceived = true;
            SendTransferAck(transfer, 0);
        }

        private void SendAck(long messageId, byte pduType, byte errorCode)
        {
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, pduType,
                0, errorCode, messageId, null, 0, 0, null, null, null, null);
        }

        private void SendTransferAck(IncomingTransfer transfer, byte errorCode)
        {
            byte pduType = transfer.OpeningChunkReceived ? DefaultProtocolDataUnit.PduTypeSubsequentChunkAck :
                DefaultProtocolDataUnit.PduTypeFirstChunkAck;
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, pduType,
                0, errorCode, transfer.MessageId, null, 0, 0, null, transfer.CancellationIndicator, null, null);
        }

        private void ResetTimeout(IncomingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.ReceiveDataTimeoutId);
            transfer.ReceiveDataTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception("receive timeout"));
                }, null);
        }

        private void AbortTransfer(IncomingTransfer transfer, Exception exception)
        {
            transfer = _incomingTransfers.TryRemove(transfer);
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
            _incomingTransfers.ForEach(transfer => DisableTransfer(transfer, causeOfReset));
            _incomingTransfers.Clear();
        }

        private void DisableTransfer(IncomingTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.ReceiveDataTimeoutId);
            transfer.ReceiveDataTimeoutId = null;
            transfer.PendingResultCancellationIndicator?.Cancel();
            transfer.PendingResultCancellationIndicator = null;
            transfer.MessageSink?.OnEndWrite(exception);
            transfer.MessageSink = null;
            transfer.MessageReceiveCallback?.Invoke(transfer.MessageReceiveCallbackState, exception);
            transfer.MessageReceiveCallback = null;
            transfer.MessageReceiveCallbackState = null;
            transfer.CancellationIndicator = null;
            transfer.PendingData = null;
            transfer.PendingAlternativePayload = null;
        }
    }
}
