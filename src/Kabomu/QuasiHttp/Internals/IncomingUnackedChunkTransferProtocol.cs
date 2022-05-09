using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class IncomingUnackedChunkTransferProtocol : IChunkTransferProtocol
    {
        private STCancellationIndicator _sendBodyPduCancellationIndicator;

        public IncomingUnackedChunkTransferProtocol(ITransferProtocol transferProtocol, Transfer transfer, byte chunkGetPduType, 
            int contentLength, string contentType)
        {
            TransferProtocol = transferProtocol;
            Transfer = transfer;
            ChunkGetPduType = chunkGetPduType;

            Body = new ChunkedTransferBody(contentLength, contentType, OnBodyChunkReadCallback,
                TransferProtocol.EventLoop);
        }

        public ITransferProtocol TransferProtocol { get; }
        public Transfer Transfer { get; }
        public byte ChunkGetPduType { get; }
        public IQuasiHttpBody Body { get; }

        public void Cancel(Exception e)
        {
            _sendBodyPduCancellationIndicator?.Cancel();
            Body.OnEndRead(e);
        }

        public void ProcessChunkGetPdu(int bytesToRead)
        {
            throw new NotImplementedException();
        }

        public void ProcessChunkRetPdu(byte[] data, int offset, int length)
        {
            try
            {
                ((ChunkedTransferBody)Body).OnDataWrite(data ?? new byte[0], offset, length);
                TransferProtocol.ResetTimeout(Transfer);
            }
            catch (Exception e)
            {
                TransferProtocol.AbortTransfer(Transfer, e);
            }
        }

        private void OnBodyChunkReadCallback(int bytesToRead)
        {
            if (bytesToRead < 0)
            {
                SendFinPdu();

                if (ChunkGetPduType == QuasiHttpPdu.PduTypeResponseChunkGet)
                {
                    TransferProtocol.AbortTransfer(Transfer, null);
                }
            }
            else 
            {
                if (ProtocolUtils.IsOperationPending(_sendBodyPduCancellationIndicator))
                {
                    TransferProtocol.AbortTransfer(Transfer, new Exception("incoming chunk transfer protocol violation"));
                    return;
                }

                SendChunkGetPdu(bytesToRead);
            }
        }

        private void SendChunkGetPdu(int bytesToRead)
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = ChunkGetPduType,
                ContentLength = bytesToRead
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

        private void SendFinPdu()
        {
            var finPduType = ChunkGetPduType == QuasiHttpPdu.PduTypeResponseChunkGet ?
                QuasiHttpPdu.PduTypeResponseFin : QuasiHttpPdu.PduTypeRequestFin;
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = finPduType
            };
            var pduBytes = pdu.Serialize();
            TransferProtocol.Transport.Write(Transfer.Connection, pduBytes, 0, pduBytes.Length, _ => { });
        }
    }
}
