using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class OutgoingUnackedChunkTransferProtocol : IChunkTransferProtocol
    {
        private STCancellationIndicator _bodyCallbackCancellationIndicator;
        private STCancellationIndicator _sendBodyPduCancellationIndicator;

        public OutgoingUnackedChunkTransferProtocol(ITransferProtocol transferProtocol, Transfer transfer, byte chunkRetPduType,
            IQuasiHttpBody body)
        {
            TransferProtocol = transferProtocol;
            Transfer = transfer;
            ChunkRetPduType = chunkRetPduType;
            Body = body;
        }

        public ITransferProtocol TransferProtocol { get; }
        public Transfer Transfer { get; }
        public byte ChunkRetPduType { get; }
        public IQuasiHttpBody Body { get; }

        public void Cancel(Exception e)
        {
            _bodyCallbackCancellationIndicator?.Cancel();
            _sendBodyPduCancellationIndicator?.Cancel();
            Body.OnEndRead(e);
        }

        public void ProcessChunkGetPdu(int bytesToRead)
        {
            if (ProtocolUtils.IsOperationPending(_bodyCallbackCancellationIndicator) ||
                ProtocolUtils.IsOperationPending(_sendBodyPduCancellationIndicator))
            {
                TransferProtocol.AbortTransfer(Transfer, new Exception("outgoing chunk transfer protocol violation"));
                return;
            }
            var cancellationIndicator = new STCancellationIndicator();
            _bodyCallbackCancellationIndicator = cancellationIndicator;
            byte[] data = new byte[bytesToRead];
            Action<Exception, int> cb = (e, bytesRead) =>
            {
                if (!cancellationIndicator.Cancelled)
                {
                    cancellationIndicator.Cancel();
                    HandleBodyChunkReadOutcome(e, data, 0, bytesRead);
                }
            };
            try
            {
                Body.OnDataRead(data, 0, data.Length, cb);
                TransferProtocol.ResetTimeout(Transfer);
            }
            catch (Exception e)
            {
                TransferProtocol.AbortTransfer(Transfer, e);
            }
        }

        public void ProcessChunkRetPdu(byte[] data, int offset, int length)
        {
            throw new NotImplementedException();
        }

        private void HandleBodyChunkReadOutcome(Exception e, byte[] data, int offset, int length)
        {
            if (e != null)
            {
                TransferProtocol.AbortTransfer(Transfer, e);
                return;
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                TransferProtocol.AbortTransfer(Transfer, new Exception("invalid outgoing body chunk"));
                return;
            }
            SendChunkRetPdu(data, offset, length);
        }

        private void SendChunkRetPdu(byte[] data, int offset, int length)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = ChunkRetPduType,
                Data = data,
                DataOffset = offset,
                DataLength = length
            };
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
            byte[] pduBytes = pdu.Serialize();
            try
            {
                TransferProtocol.Transport.Write(Transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
            }
            catch (Exception e)
            {
                TransferProtocol.AbortTransfer(Transfer, e);
            }
        }

        private void HandleSendPduOutcome(Exception e)
        {
            if (e != null)
            {
                TransferProtocol.AbortTransfer(Transfer, e);
                return;
            }
        }
    }
}
