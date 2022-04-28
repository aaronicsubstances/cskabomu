using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ReceiveProtocol
    {
        private readonly Dictionary<int, QuasiHttpMessageTransfer> _incomingTransfers = 
            new Dictionary<int, QuasiHttpMessageTransfer>();
        private readonly Action<object, Exception> NullCb = (s, e) => { };

        public IQuasiHttpTransport Transport { get; set; }
        public int DefaultTimeoutMillis { get; set; }
        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }
        public IQuasiHttpApplication Application { get; set; }

        public void ProcessIncomingRequest(QuasiHttpPdu pdu, object connectionHandle)
        {
            var transfer = new QuasiHttpMessageTransfer
            {
                Pdu = pdu,
                TimeoutMillis = DefaultTimeoutMillis,
                ReplyConnectionHandle = connectionHandle
            };
            _incomingTransfers.Add(pdu.RequestId, transfer);
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
                SendTransferAck(transfer, ErrorCodeGeneral);
                return;
            }

            if (transfer.TerminatingChunkSeen)
            {
                AbortTransfer(transfer, null);
            }

            SendTransferAck(transfer, 0);
            transfer.OpeningChunkReceived = true;
        }

        private void SendAck(object connectionHandle, long messageId, byte pduType, byte errorCode)
        {
            QuasiHttpTransport.BeginSendPdu(connectionHandle, DefaultProtocolDataUnit.Version01, pduType,
                0, errorCode, messageId, null, 0, 0, null, null, NullCb, null);
        }

        private void SendTransferAck(IncomingTransfer transfer, byte errorCode)
        {
            var cancellationIndicator = new STCancellationIndicator();
            transfer.PendingResultCancellationIndicator = cancellationIndicator;
            Action<object, Exception> cb = (object cbState, Exception error) =>
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
            byte pduType = transfer.OpeningChunkReceived ? DefaultProtocolDataUnit.PduTypeSubsequentChunkAck :
                DefaultProtocolDataUnit.PduTypeFirstChunkAck;
            object replyConnectionHandle = transfer.ReplyConnectionHandle;
            transfer.ReplyConnectionHandle = null;
            QuasiHttpTransport.BeginSendPdu(replyConnectionHandle, DefaultProtocolDataUnit.Version01, pduType,
                0, errorCode, transfer.MessageId, null, 0, 0, null, transfer.CancellationIndicator, cb, null);
        }

        private void ProcessSendAckOutcome(IncomingTransfer transfer, Exception ex)
        {
            if (ex != null)
            {
                AbortTransfer(transfer, ex);
                return;
            }
        }

        private void ResetTimeout(QuasiHttpMessageTransfer transfer)
        {
            transfer.RequestTimeoutId = EventLoop.ScheduleTimeout(transfer.TimeoutMillis,
                _ =>
                {
                    AbortTransfer(transfer, new Exception(GenerateErrorMessage(ErrorCodeReceiveTimeout, null)));
                }, null);
        }

        private void AbortTransfer(QuasiHttpMessageTransfer transfer, Exception exception)
        {
            if (!_incomingTransfers.Remove(transfer.Pdu.RequestId))
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
            foreach (var transfer in _incomingTransfers.Values)
            {
                DisableTransfer(transfer, causeOfReset);
            }
            _incomingTransfers.Clear();
        }

        private void DisableTransfer(QuasiHttpMessageTransfer transfer, Exception exception)
        {
            EventLoop.CancelTimeout(transfer.RequestTimeoutId);
            transfer.RequestTimeoutId = null;
            transfer.PendingResultCancellationIndicator?.Cancel();
            transfer.PendingResultCancellationIndicator = null;
            transfer.RequestCallback?.Invoke(exception, null);
            transfer.RequestCallback = null;
            transfer.ReplyConnectionHandle = null;
        }
    }
}
