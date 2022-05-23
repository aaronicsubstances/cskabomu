using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Internals.MessageOrientedProtocols
{
    internal class IncomingChunkTransferProtocol
    {
        private STCancellationIndicator _sendBodyPduCancellationIndicator;

        public IncomingChunkTransferProtocol(IQuasiHttpTransport transport, IEventLoopApi eventLoop,
            object connection, Action<Exception> abortCallback, 
            byte chunkGetPduType, int contentLength, string contentType)
        {
            Transport = transport;
            EventLoop = eventLoop;
            Connection = connection;
            AbortCallback = abortCallback;
            ChunkGetPduType = chunkGetPduType;

            Body = new ChunkTransferBody(contentLength, contentType, OnBodyChunkReadCallback, OnBodyEndReadCallback,
                eventLoop);
        }

        public IQuasiHttpTransport Transport { get; }
        public IEventLoopApi EventLoop { get; }
        public object Connection { get; }
        public Action<Exception> AbortCallback { get; }
        public byte ChunkGetPduType { get; }
        public ChunkTransferBody Body { get; }

        public void Cancel(Exception e)
        {
            _sendBodyPduCancellationIndicator?.Cancel();
            Body.OnEndRead(e);
        }

        public void ProcessChunkRetPdu(byte[] data, int offset, int length)
        {
            Body.OnDataWrite(data ?? new byte[0], offset, length);
        }

        private void OnBodyChunkReadCallback(int bytesToRead)
        {
            SendChunkGetPdu(bytesToRead);
        }

        private void OnBodyEndReadCallback()
        {
            if (ChunkGetPduType == TransferPdu.PduTypeResponseChunkGet)
            {
                SendFinPdu();
                AbortCallback.Invoke(null);
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
                EventLoop.PostCallback(_ =>
                {
                    if (!cancellationIndicator.Cancelled)
                    {
                        cancellationIndicator.Cancel();
                        HandleSendPduOutcome(e);
                    }
                }, null);
            };
            byte[] pduBytes = pdu.Serialize();
            Transport.WriteBytesOrSendMessage(Connection, pduBytes, 0, pduBytes.Length, cb);
        }

        private void HandleSendPduOutcome(Exception e)
        {
            if (e != null)
            {
                AbortCallback.Invoke(e);
                return;
            }
        }

        private void SendFinPdu()
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = TransferPdu.PduTypeFin
            };
            var pduBytes = pdu.Serialize();
            Transport.WriteBytesOrSendMessage(Connection, pduBytes, 0, pduBytes.Length, _ => { });
        }
    }
}
