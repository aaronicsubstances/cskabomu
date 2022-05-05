using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals
{
    internal class IncomingChunkTransferProtocol
    {
        private static readonly Action<Exception> NullCallback = _ => { };

        private readonly Action<bool> ChunkReadCallback;

        private STCancellationIndicator _sendBodyPduCancellationIndicator;
        private object _replyConnectionHandle;

        public IncomingChunkTransferProtocol(ITransferProtocol transferProtocol, ITransfer transfer, byte chunkGetPduType, 
            int contentLength, string contentType, object initialReplyConnectionHandle)
        {
            TransferProtocol = transferProtocol;
            Transfer = transfer;
            ChunkGetPduType = chunkGetPduType;
            Body = new ChunkedTransferBody(contentLength, contentType, ChunkReadCallback,
                TransferProtocol.EventLoop);
            _replyConnectionHandle = initialReplyConnectionHandle;

            ChunkReadCallback = OnBodyChunkReadCallback;
        }

        public ITransferProtocol TransferProtocol { get; }
        public ITransfer Transfer { get; }
        public byte ChunkGetPduType { get; }
        public ChunkedTransferBody Body { get; }

        public void Cancel(Exception e)
        {
            _sendBodyPduCancellationIndicator?.Cancel();
            Body.OnEndRead(e);
        }

        public void ProcessChunkRetPdu(byte[] data, int offset, int length, object connectionHandle)
        {
            try
            {
                Body.OnDataWrite(data, offset, length);
                Transfer.ResetTimeout();
                _replyConnectionHandle = connectionHandle;
            }
            catch (Exception e)
            {
                Transfer.Abort(e);
            }
        }

        private void OnBodyChunkReadCallback(bool read)
        {
            if (read)
            {
                if (ProtocolUtils.IsOperationPending(_sendBodyPduCancellationIndicator))
                {
                    Transfer.Abort(new Exception("incoming chunk transfer protocol violation"));
                    return;
                }

                SendChunkGetPdu();
            }
            else
            {
                SendFinPdu();

                if (ChunkGetPduType == QuasiHttpPdu.PduTypeResponseChunkGet)
                {
                    Transfer.Abort(null);
                }
            }
        }

        private void SendChunkGetPdu()
        {
            var pdu = new QuasiHttpPdu
            {
                Version = QuasiHttpPdu.Version01,
                PduType = ChunkGetPduType,
                RequestId = Transfer.RequestId
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
            TransferProtocol.Transport.SendPdu(pduBytes, 0, pduBytes.Length, _replyConnectionHandle, cb);
        }

        private void HandleSendPduOutcome(Exception e)
        {
            if (e != null)
            {
                Transfer.Abort(e);
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
                PduType = finPduType,
                RequestId = Transfer.RequestId
            };
            var pduBytes = pdu.Serialize();
            TransferProtocol.Transport.SendPdu(pduBytes, 0, pduBytes.Length, _replyConnectionHandle, NullCallback);
        }
    }
}
