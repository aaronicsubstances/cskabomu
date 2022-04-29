using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class SendProtocol
    {
        private readonly Dictionary<int, QuasiHttpMessageTransfer> _outgoingTransfers =
            new Dictionary<int, QuasiHttpMessageTransfer>();
        private int _requestIdGenerator;

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public void ProcessOutgoingRequest(QuasiHttpRequestMessage request, QuasiHttpPostOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb)
        {
            var pdu = new QuasiHttpPdu
            {
                Request = request,
                RequestId = ++_requestIdGenerator
            };
            var transfer = new QuasiHttpMessageTransfer
            {
                Pdu = pdu,
                RequestCallback = cb
            };
            if (options != null)
            {
                transfer.TimeoutMillis = options.TimeoutMillis;
            }
            if (transfer.TimeoutMillis <= 0)
            {
                transfer.TimeoutMillis = DefaultTimeoutMillis;
            }

            _outgoingTransfers.Add(transfer.Pdu.RequestId, transfer);
            ResetTimeout(transfer);
            BeginReadMessageSource(transfer);
        }

        public void ProcessIncomingResponse(QuasiHttpPdu pdu, object connectionHandle)
        {
            throw new NotImplementedException();
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
                    ProcessSourceResult(transfer, error, data, offset, length, fallbackPayload, hasMore,
                        cancellationIndicator);
                }, null);
            };
            transfer.MessageSource.OnDataRead(cb, null);
        }

        private void ProcessSourceResult(OutgoingTransfer transfer, Exception error,
            byte[] data, int offset, int length, object fallbackPayload, bool hasMore,
            STCancellationIndicator cancellationIndicator)
        {
            if (cancellationIndicator.Cancelled)
            {
                return;
            }
            cancellationIndicator.Cancel();
            transfer.AwaitingSourceResult = false;
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
                    ProcessSendDataOutcome(transfer, ex, cancellationIndicator);
                }, null);
            };
            byte flags = DefaultProtocolDataUnit.ComputeFlags(transfer.StartedAtReceiver,
                !transfer.TerminatingChunkSeen);
            byte pduType = transfer.OpeningChunkSent ? DefaultProtocolDataUnit.PduTypeSubsequentChunk :
                DefaultProtocolDataUnit.PduTypeFirstChunk;
            object connectionHandle = transfer.RequestConnectionHandle;
            transfer.RequestConnectionHandle = null;
            Transport.BeginSendPdu(connectionHandle, DefaultProtocolDataUnit.Version01, pduType,
                flags, 0, transfer.MessageId, transfer.PendingData, transfer.PendingDataOffset,
                transfer.PendingDataLength, transfer.PendingFallbackPayload,
                transfer.CancellationIndicator, cb, null);
        }

        private void ProcessSendDataOutcome(OutgoingTransfer transfer, Exception ex, 
            STCancellationIndicator cancellationIndicator)
        {
            if (cancellationIndicator.Cancelled)
            {
                return;
            }
            cancellationIndicator.Cancel();
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
            if (transfer == null)
            {
                // ignore.
                return;
            }
            transfer.RequestConnectionHandle = connectionHandle;
            if (transfer.AwaitingSourceResult)
            {
                AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeProtocolViolation, null) +
                    " (stop and wait violation)"));
                return;
            }
            if (transfer.OpeningChunkSent)
            {
                AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeProtocolViolation, null) +
                    " (expected subsequent ack instead)"));
                return;
            }
            if (errorCode != 0)
            {
                AbortTransfer(transfer, new Exception(GenerateErrorMessage(
                    errorCode, $"{errorCode}:remote peer error")));
                return;
            }

            AbortTransfer(transfer, null);
        }

        private void ResetTimeout(QuasiHttpMessageTransfer transfer)
        {
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeSendTimeout, null)));
                }, null);
        }

        private void AbortTransfer(QuasiHttpMessageTransfer transfer, Exception exception)
        {
            if (!_outgoingTransfers.Remove(transfer.Pdu.RequestId))
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
            foreach (var transfer in _outgoingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _outgoingTransfers.Clear();
        }

        private void DisableTransfer(QuasiHttpMessageTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = null;
            transfer.PendingResultCancellationIndicator?.Cancel();
            transfer.PendingResultCancellationIndicator = null;
            transfer.RequestCallback?.Invoke(exception, null);
            transfer.RequestCallback = null;
        }
    }
}
