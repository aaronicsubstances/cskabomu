using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;
using static Kabomu.Common.Components.DefaultMessageTransferManager;

namespace Kabomu.Common.Internals
{
    internal class ReceiveProtocol
    {
        private readonly ITransferCollection<IncomingTransfer> _incomingTransfers = 
            new DefaultTransferCollection<IncomingTransfer>();

        public IQpcFacility QpcService { get; set; }
        public IMessageSinkFactory MessageSinkFactory { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void BeginReceive(IMessageSink msgSink, long msgIdAtReceiver, 
            IMessageTransferOptions options, Action<object, Exception> cb, object cbState)
        {
            var transfer = new IncomingTransfer
            {
                MessageId = msgIdAtReceiver,
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
        }

        private void ProcessReceive(IncomingTransfer transfer)
        {
            if (!_incomingTransfers.TryAdd(transfer))
            {
                DisableTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeMessageIdInUse, null)));
                return;
            }
            ResetTimeout(transfer);
        }

        public void OnReceiveFirstChunk(object connectionHandle, byte flags, long messageId,
            byte[] data, int offset, int length, object fallbackPayload)
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
                    SendAck(connectionHandle, messageId, DefaultProtocolDataUnit.PduTypeFirstChunkAck,
                        ErrorCodeMessageIdNotFound);
                    return;
                }
                transfer.ReplyConnectionHandle = connectionHandle;
                if (transfer.PendingResultCancellationIndicator != null)
                {
                    AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeProtocolViolation, null) +
                        " (stop and wait violation)"));
                    SendTransferAck(transfer, ErrorCodeProtocolViolation, false);
                    return;
                }
                if (transfer.OpeningChunkReceived)
                {
                    // Intepret as an illegal attempt to use a first chunk to continue a transfer.
                    AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeProtocolViolation, null) +
                        " (first chunk cannot continue a transfer)"));
                    SendTransferAck(transfer, ErrorCodeProtocolViolation, false);
                    return;
                }
            }
            else
            {
                if (transfer != null)
                {
                    // Intepret as a valid attempt to reuse a message id for a new transfer started by sender.
                    // Abort existing transfer and create a new one to replace it.
                    AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeAbortedBySender, null)));
                }
                transfer = new IncomingTransfer
                {
                    MessageId = messageId,
                    StartedAtReceiver = false,
                    TimeoutMillis = DefaultTimeoutMillis,
                    ReplyConnectionHandle = connectionHandle
                };
                if (!_incomingTransfers.TryAdd(transfer))
                {
                    DisableTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeGeneral, null) +
                        " (internal failure in adding to transfer collection)"));
                    return;
                }
                ResetTimeout(transfer);
            }

            bool hasMore = DefaultProtocolDataUnit.IsHasMoreFlagPresent(flags);
            transfer.TerminatingChunkSeen = !hasMore;
            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingFallbackPayload = fallbackPayload;
            if (startedAtReceiver)
            {
                BeginWriteMessageSink(transfer);
            }
            else
            {
                BeginCreateMessageSink(transfer);
            }
        }

        public void OnReceiveSubsequentChunk(object connectionHandle, byte flags, long messageId,
            byte[] data, int offset, int length, object fallbackPayload)
        {
            bool startedAtReceiver = DefaultProtocolDataUnit.IsStartedAtReceiverFlagPresent(flags);
            IncomingTransfer transfer = _incomingTransfers.TryGet(new IncomingTransfer
            {
                MessageId = messageId,
                StartedAtReceiver = startedAtReceiver
            });
            if (transfer == null)
            {
                SendAck(connectionHandle, messageId, DefaultProtocolDataUnit.PduTypeSubsequentChunkAck,
                    ErrorCodeMessageIdNotFound);
                return;
            }
            transfer.ReplyConnectionHandle = connectionHandle;
            if (transfer.PendingResultCancellationIndicator != null)
            {
                AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeProtocolViolation, null) +
                    " (stop and wait violation)"));
                SendTransferAck(transfer, ErrorCodeProtocolViolation, false);
                return;
            }
            if (!transfer.OpeningChunkReceived)
            {
                // Intepret as an illegal attempt to use a subsequent chunk to start a transfer.
                AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeProtocolViolation, null) +
                    " (subsequent chunk cannot start a transfer)"));
                SendTransferAck(transfer, ErrorCodeProtocolViolation, false);
                return;
            }

            bool hasMore = DefaultProtocolDataUnit.IsHasMoreFlagPresent(flags);
            transfer.TerminatingChunkSeen = !hasMore;
            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingFallbackPayload = fallbackPayload;

            BeginWriteMessageSink(transfer);
        }

        private void BeginCreateMessageSink(IncomingTransfer transfer)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.PendingResultCancellationIndicator = cancellationIndicator;
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
            MessageSinkFactory.CreateMessageSink(cb, null);
        }

        private void ProcessSinkCreationResult(IncomingTransfer transfer, Exception error, 
            IMessageSink sink, ICancellationIndicator cancellationIndicator, Action<object, Exception> recvCb, object recvCbState)
        {
            if (error != null)
            {
                AbortTransfer(transfer, error);
                SendTransferAck(transfer, 1, false);
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
            transfer.PendingResultCancellationIndicator = cancellationIndicator;
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
            transfer.MessageSink.OnDataWrite(transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingFallbackPayload, !transfer.TerminatingChunkSeen, cb, null);
        }

        private void ProcessSinkResult(IncomingTransfer transfer, Exception error)
        {
            if (error != null)
            {
                AbortTransfer(transfer, error);
                SendTransferAck(transfer, 1, false);
                return;
            }

            transfer.OpeningChunkReceived = true;
            SendTransferAck(transfer, 0, !transfer.TerminatingChunkSeen);
        }

        private void SendAck(object connectionHandle, long messageId, byte pduType, byte errorCode)
        {
            QpcService.BeginSendPdu(connectionHandle, DefaultProtocolDataUnit.Version01, pduType,
                0, errorCode, messageId, null, 0, 0, null, null, null, null);
        }

        private void SendTransferAck(IncomingTransfer transfer, byte errorCode, bool waitForOutcome)
        {
            Action<object, Exception> cb = null;
            if (waitForOutcome)
            {
                var cancellationIndicator = new STCancellationIndicator();
                transfer.PendingResultCancellationIndicator = cancellationIndicator;
                cb = (object cbState, Exception error) =>
                {
                    EventLoop.PostCallback(_ =>
                    {
                        if (transfer.PendingResultCancellationIndicator == cancellationIndicator)
                        {
                            transfer.PendingResultCancellationIndicator = null;
                            ProcessSendAckOutcome(transfer, error);
                        }
                    }, null);
                };
            }
            byte pduType = transfer.OpeningChunkReceived ? DefaultProtocolDataUnit.PduTypeSubsequentChunkAck :
                DefaultProtocolDataUnit.PduTypeFirstChunkAck;
            object replyConnectionHandle = transfer.ReplyConnectionHandle;
            transfer.ReplyConnectionHandle = null;
            QpcService.BeginSendPdu(replyConnectionHandle, DefaultProtocolDataUnit.Version01, pduType,
                0, errorCode, transfer.MessageId, null, 0, 0, null, transfer.CancellationIndicator, null, null);
        }

        private void ProcessSendAckOutcome(IncomingTransfer transfer, Exception ex)
        {
            if (ex != null)
            {
                AbortTransfer(transfer, ex);
                return;
            }
        }

        private void ResetTimeout(IncomingTransfer transfer)
        {
            EventLoop.CancelTimeout(transfer.ReceiveDataTimeoutId);
            transfer.ReceiveDataTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeReceiveTimeout, null)));
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
                exception = new Exception(GenerateErrorMessage(ErrorCodeCancelled, null));
            }
            DisableTransfer(transfer, exception);
        }

        internal void ProcessReset(Exception causeOfReset)
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
            transfer.PendingFallbackPayload = null;
            transfer.ReplyConnectionHandle = null;
        }
    }
}
