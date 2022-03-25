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
                MessageReceiveCallbackState = cbState
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
            _incomingTransfers.Add(transfer.MessageId, transfer);
            ResetTimeout(transfer);
        }

        public void OnReceiveDataPdu(byte flags, long messageId, byte[] data, int offset,
            int length, object alternativePayload)
        {
            bool continueTransfer = (flags & (1 << 0)) == 1;
            bool hasMore = (flags & (1 << 1)) == 1;
            IncomingTransfer transfer;
            if (continueTransfer)
            {
                if (!_incomingTransfers.ContainsKey(messageId))
                {
                    SendAck(messageId, 1);
                    return;
                }
                transfer = _incomingTransfers[messageId];
            }
            else
            {
                if (_incomingTransfers.ContainsKey(messageId))
                {
                    SendAck(messageId, 1);
                    return;
                }
                transfer = new IncomingTransfer
                {
                    MessageId = messageId,
                    TimeoutMillis = DefaultTimeoutMillis
                };
                _incomingTransfers.Add(messageId, transfer);
                ResetTimeout(transfer);

            }
            if (transfer.AwaitingPendingResult)
            {
                return;
            }
            transfer.PendingData = data;
            transfer.PendingDataOffset = offset;
            transfer.PendingDataLength = length;
            transfer.PendingAlternativePayload = alternativePayload;
            transfer.TerminatingChunkSeen = hasMore;
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
            var pendingResultCancellationHandle = new SimpleCancellationHandle();
            transfer.PendingResultCancellationHandle = pendingResultCancellationHandle;
            MessageSinkCreationCallback cb = (object cbState, Exception error, IMessageSink sink,
                Action<object, Exception> recvCb, object recvCbState) =>
            {
                if (!pendingResultCancellationHandle.Cancelled)
                {
                    pendingResultCancellationHandle.Cancel();
                    EventLoop.PostCallback(_ =>
                        ProcessSinkCreationResult(transfer, error, sink, recvCb, recvCbState), null);
                }
            };
            transfer.AwaitingPendingResult = true;
            MessageSinkFactory.CreateMessageSink(transfer.MessageId, cb, null);
        }

        private void ProcessSinkCreationResult(IncomingTransfer transfer, Exception error, 
            IMessageSink sink, Action<object, Exception> recvCb, object recvCbState)
        {
            transfer.AwaitingPendingResult = false;
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }

            transfer.MessageSink = sink;
            transfer.MessageReceiveCallback = recvCb;
            transfer.MessageReceiveCallbackState = recvCbState;

            ResetTimeout(transfer);
            BeginWriteMessageSink(transfer);
        }

        private void BeginWriteMessageSink(IncomingTransfer transfer)
        {
            var pendingResultCancellationHandle = new SimpleCancellationHandle();
            transfer.PendingResultCancellationHandle = pendingResultCancellationHandle;
            MessageSinkCallback cb = (object cbState, Exception error) =>
            {
                if (!pendingResultCancellationHandle.Cancelled)
                {
                    pendingResultCancellationHandle.Cancel();
                    EventLoop.PostCallback(_ =>
                        ProcessSinkResult(transfer, error), null);
                }
            };
            transfer.AwaitingPendingResult = true;
            transfer.MessageSink.OnDataWrite(transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingAlternativePayload, transfer.TerminatingChunkSeen, cb, null);
        }

        private void ProcessSinkResult(IncomingTransfer transfer, Exception error)
        {
            transfer.AwaitingPendingResult = false;
            if (error != null)
            {
                AbortTransfer(transfer, error);
                return;
            }

            SendAck(transfer.MessageId, 0);

            if (transfer.TerminatingChunkSeen)
            {
                AbortTransfer(transfer, null);
            }
        }

        private void SendAck(long messageId, byte errorCode)
        {
            QpcService.BeginSend(DefaultProtocolDataUnit.Version01, DefaultProtocolDataUnit.PduTypeDataAck,
                0, errorCode, messageId, null, 0, 0, null, null, null, null);
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
            _incomingTransfers.Remove(transfer.MessageId);
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
            transfer.PendingResultCancellationHandle?.Cancel();
            transfer.MessageSink.OnEndWrite(exception);
            transfer.MessageReceiveCallback?.Invoke(transfer.MessageReceiveCallbackState, exception);
            transfer.MessageSink = null;
            transfer.CancellationHandle = null;
            transfer.PendingData = null;
            transfer.PendingAlternativePayload = null;
        }
    }
}
