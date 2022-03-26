using Kabomu.Common.Abstractions;
using Kabomu.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Internals
{
    internal class ReceiveProtocol
    {
        private readonly Dictionary<long, IncomingTransfer> _incomingTransfers;

        public ReceiveProtocol()
        {
            _incomingTransfers = new Dictionary<long, IncomingTransfer>();
        }

        public IQpcFacility QpcService { get; set; } // absolutely required.

        public IMessageSinkFactory MessageSinkFactory { get; set; } // absolutely required.

        public int DefaultTimeoutMillis { get; set; } // absolutely required.

        public IEventLoopApi EventLoop { get; set; } // absolutely required

        public IRecyclingFactory RecyclingFactory { get; set; }

        public void BeginReceive(IMessageSink msgSink, IMessageTransferOptions options, Action<object, Exception> cb, object cbState)
        {
            var transfer = new IncomingTransfer
            {
                MessageId = options.MessageId,
                TimeoutMillis = options.TimeoutMillis,
                CancellationHandle = options.CancellationHandle,
                MessageSink = msgSink,
                MessageReceiveCallback = cb,
                MessageReceiveCallbackState = cbState,
                NextPendingResultId = 0
            };
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
            if (_incomingTransfers.ContainsKey(transfer.MessageId))
            {
                // Intepret as an attempt to reuse a message id for a new transfer manually started at receiver.
                // Abort existing transfer and create a new one to replace it.
                AbortTransfer(_incomingTransfers[transfer.MessageId], new Exception("message id reuse"));
            }
            _incomingTransfers.Add(transfer.MessageId, transfer);
            transfer.CancellationHandle?.TryAddCancellationListener(_ =>
            {
                EventLoop.PostCallback(_ =>
                    AbortTransfer(transfer, new Exception("cancelled")), null);
            }, null);
            ResetTimeout(transfer);
        }

        public void OnReceiveDataPdu(byte flags, long messageId, byte[] data, int offset,
            int length, object alternativePayload)
        {
            bool continueTransfer = DefaultProtocolDataUnit.IsContinueTransferFlagPresent(flags);
            bool hasMore = DefaultProtocolDataUnit.IsHasMoreFlagPresent(flags);
            bool receiveAlreadyStarted = DefaultProtocolDataUnit.IsReceiveAlreadyStartedFlagPresent(flags);
            IncomingTransfer transfer;
            if (continueTransfer)
            {
                if (!_incomingTransfers.ContainsKey(messageId))
                {
                    // ignore.
                    return;
                }
                transfer = _incomingTransfers[messageId];
                if (!transfer.OpeningChunkSeen)
                {
                    // Intepret as an illegal attempt to use a first chunk to continue a transfer.
                    AbortTransfer(transfer, new Exception("protocol violation: first chunk cannot continue a transfer"));
                    SendTransferAck(transfer, 1);
                    return;
                }
            }
            else
            {
                if (receiveAlreadyStarted)
                {
                    if (!_incomingTransfers.ContainsKey(messageId))
                    {
                        SendAck(messageId, 1);
                        return;
                    }
                    transfer = _incomingTransfers[messageId];
                    if (transfer.OpeningChunkSeen)
                    {
                        // Intepret as an attempt to reuse a message id for a new transfer automatically started at receiver.
                        // Abort existing transfer and sending back an ack to abort at sender too.
                        AbortTransfer(transfer, new Exception("message id reuse"));
                        SendTransferAck(transfer, 1);
                        return;
                    }
                }
                else
                {
                    if (_incomingTransfers.ContainsKey(messageId))
                    {
                        // Intepret as an attempt to reuse a message id for a new transfer started by sender.
                        // Abort existing transfer and create a new one to replace it.
                        AbortTransfer(_incomingTransfers[messageId], new Exception("message id reuse"));
                    }
                    transfer = new IncomingTransfer
                    {
                        MessageId = messageId,
                        TimeoutMillis = DefaultTimeoutMillis,
                        NextPendingResultId = 0
                    };
                    _incomingTransfers.Add(messageId, transfer);
                    ResetTimeout(transfer);
                }
            }
            if (transfer.AwaitingPendingResult)
            {
                AbortTransfer(transfer, new Exception("stop and wait violation"));
                SendTransferAck(transfer, 1);
                return;
            }

            transfer.OpeningChunkSeen = true;
            transfer.TerminatingChunkSeen = !hasMore;
            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingAlternativePayload = alternativePayload;
            if (!continueTransfer)
            {
                BeginCreateMessageSink(transfer);
            }
            else
            {
                BeginWriteMessageSink(transfer);
            }
        }

        private void BeginCreateMessageSink(IncomingTransfer transfer)
        {
            var pendingResultId = transfer.NextPendingResultId;
            MessageSinkCreationCallback cb = (object cbState, Exception error, IMessageSink sink, ICancellationHandle cancellationHandle,
                Action<object, Exception> recvCb, object recvCbState) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.NextPendingResultId == pendingResultId)
                    {
                        transfer.NextPendingResultId++;
                        ProcessSinkCreationResult(transfer, error, sink, cancellationHandle, recvCb, recvCbState);
                    }
                }, null);
            };
            transfer.AwaitingPendingResult = true;
            MessageSinkFactory.CreateMessageSink(transfer.MessageId, cb, null);
        }

        private void ProcessSinkCreationResult(IncomingTransfer transfer, Exception error, 
            IMessageSink sink, ICancellationHandle cancellationHandle, Action<object, Exception> recvCb, object recvCbState)
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
            transfer.CancellationHandle = cancellationHandle;

            ResetTimeout(transfer);
            BeginWriteMessageSink(transfer);
        }

        private void BeginWriteMessageSink(IncomingTransfer transfer)
        {
            var pendingResultId = transfer.NextPendingResultId;
            MessageSinkCallback cb = (object cbState, Exception error) =>
            {
                EventLoop.PostCallback(_ =>
                {
                    if (transfer.NextPendingResultId == pendingResultId)
                    {
                        transfer.NextPendingResultId++;
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

            if (transfer.TerminatingChunkSeen)
            {
                AbortTransfer(transfer, null);
            }

            // in any case send back final ack.
            SendTransferAck(transfer, 0);
        }

        private void SendAck(long messageId, byte errorCode)
        {
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, DefaultProtocolDataUnit.PduTypeDataAck,
                0, errorCode, messageId, null, 0, 0, null, null, null, null);
        }

        private void SendTransferAck(IncomingTransfer transfer, byte errorCode)
        {
            Action<object, Exception> nullCb = (s, e) => { };
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, DefaultProtocolDataUnit.PduTypeDataAck,
                0, errorCode, transfer.MessageId, null, 0, 0, null, transfer.CancellationHandle, nullCb, null);
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
            if (!_incomingTransfers.Remove(transfer.MessageId))
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
            foreach (var transfer in _incomingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _incomingTransfers.Clear();
        }

        private void DisableTransfer(IncomingTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.ReceiveDataTimeoutId);
            transfer.NextPendingResultId = -1;
            transfer.MessageSink?.OnEndWrite(exception);
            transfer.MessageReceiveCallback?.Invoke(transfer.MessageReceiveCallbackState, exception);
            transfer.CancellationHandle?.Cancel();
        }
    }
}
