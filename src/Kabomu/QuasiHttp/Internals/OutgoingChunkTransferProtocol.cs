﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class OutgoingChunkTransferProtocol
    {
        private STCancellationIndicator _bodyCallbackCancellationIndicator;
        private STCancellationIndicator _sendBodyPduCancellationIndicator;

        public OutgoingChunkTransferProtocol(ITransferProtocol transferProtocol, ITransfer transfer, byte chunkRetPduType,
            IQuasiHttpBody body)
        {
            TransferProtocol = transferProtocol;
            Transfer = transfer;
            ChunkRetPduType = chunkRetPduType;
            Body = body;
        }

        public ITransferProtocol TransferProtocol { get; }
        public ITransfer Transfer { get; }
        public byte ChunkRetPduType { get; }
        public IQuasiHttpBody Body { get; }

        public void Cancel(Exception e)
        {
            _bodyCallbackCancellationIndicator?.Cancel();
            _sendBodyPduCancellationIndicator?.Cancel();
            Body.OnEndRead(e);
        }

        public void ProcessChunkGetPdu(object connectionHandle)
        {
            if (ProtocolUtils.IsOperationPending(_bodyCallbackCancellationIndicator) ||
                ProtocolUtils.IsOperationPending(_sendBodyPduCancellationIndicator))
            {
                Transfer.Abort(new Exception("outgoing chunk transfer protocol violation"));
                return;
            }
            var cancellationIndicator = new STCancellationIndicator();
            _bodyCallbackCancellationIndicator = cancellationIndicator;
            QuasiHttpBodyCallback cb = (e, data, offset, length) =>
            {
                if (!cancellationIndicator.Cancelled)
                {
                    cancellationIndicator.Cancel();
                    HandleBodyChunk(e, data, offset, length, connectionHandle);
                }
            };
            try
            {
                Body.OnDataRead(cb);
                Transfer.ResetTimeout();
            }
            catch (Exception e)
            {
                cancellationIndicator.Cancel();
                HandleBodyChunk(e, null, 0, 0, null);
            }
        }

        private void HandleBodyChunk(Exception e, byte[] data, int offset, int length, object replyConnectionHandle)
        {
            if (e != null)
            {
                Transfer.Abort(e);
                return;
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                Transfer.Abort(new Exception("invalid outgoing body chunk"));
                return;
            }
            SendChunkRetPdu(data, offset, length, replyConnectionHandle);
        }

        private void SendChunkRetPdu(byte[] data, int offset, int length, object replyConnectionHandle)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = ChunkRetPduType,
                RequestId = Transfer.RequestId,
                EmbeddedBody = data,
                EmbeddedBodyOffset = offset,
                ContentLength = length
            };
            var pduBytes = pdu.Serialize();
            var cancellationIndicator = new STCancellationIndicator();
            _sendBodyPduCancellationIndicator = cancellationIndicator;
            Action<Exception> cb = e =>
            {
                TransferProtocol.EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendPduOutcome(e);
                    }
                }, null);
            };
            TransferProtocol.Transport.SendPdu(pduBytes, 0, pduBytes.Length, replyConnectionHandle, cb);
        }

        private void HandleSendPduOutcome(Exception e)
        {
            if (e != null)
            {
                Transfer.Abort(e);
                return;
            }
        }
    }
}
