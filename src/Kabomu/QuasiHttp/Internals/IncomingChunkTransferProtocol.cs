using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class IncomingChunkTransferProtocol : IChunkTransferProtocol
    {
        private STCancellationIndicator _sendBodyPduCancellationIndicator;

        public IncomingChunkTransferProtocol(ITransferProtocol transferProtocol, Transfer transfer, byte chunkGetPduType, 
            int contentLength, string contentType)
        {
            TransferProtocol = transferProtocol;
            Transfer = transfer;
            ChunkGetPduType = chunkGetPduType;

            Body = new ChunkTransferBody(contentLength, contentType, OnBodyChunkReadCallback, OnBodyEndReadCallback,
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
                ((ChunkTransferBody)Body).OnDataWrite(data ?? new byte[0], offset, length);
                TransferProtocol.ResetTimeout(Transfer);
            }
            catch (Exception e)
            {
                TransferProtocol.AbortTransfer(Transfer, e);
            }
        }

        private void OnBodyChunkReadCallback(int bytesToRead)
        {
            SendChunkGetPdu(bytesToRead);
        }

        private void OnBodyEndReadCallback()
        {
            SendFinPdu();

            if (ChunkGetPduType == TransferPdu.PduTypeResponseChunkGet)
            {
                TransferProtocol.AbortTransfer(Transfer, null);
            }
        }

        private void SendChunkGetPdu(int bytesToRead)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
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
                TransferProtocol.Transport.WriteBytesOrSendMessage(Transfer.Connection, pduBytes, 0, pduBytes.Length, cb);
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
            var finPduType = ChunkGetPduType == TransferPdu.PduTypeResponseChunkGet ?
                TransferPdu.PduTypeResponseFin : TransferPdu.PduTypeRequestFin;
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = finPduType
            };
            var pduBytes = pdu.Serialize();
            TransferProtocol.Transport.WriteBytesOrSendMessage(Transfer.Connection, pduBytes, 0, pduBytes.Length, _ => { });
        }
    }
}
