using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Internals
{
    internal class IncomingChunkTransferProtocol
    {
        private STCancellationIndicator _sendBodyPduCancellationIndicator;

        public IncomingChunkTransferProtocol(IQuasiHttpTransport transport, IMutexApi mutex,
            object connection, Action<Exception> abortCallback, 
            byte chunkGetPduType, int contentLength, string contentType)
        {
            Transport = transport;
            Mutex = mutex;
            Connection = connection;
            AbortCallback = abortCallback;
            ChunkGetPduType = chunkGetPduType;

            Body = new ChunkTransferBody(contentLength, contentType, OnBodyChunkReadCallback, OnBodyEndReadCallback);
        }

        public IQuasiHttpTransport Transport { get; }
        public IMutexApi Mutex { get; }
        public object Connection { get; }
        public Action<Exception> AbortCallback { get; }
        public byte ChunkGetPduType { get; }
        public ChunkTransferBody Body { get; }

        public void Cancel(Exception e)
        {
            _sendBodyPduCancellationIndicator?.Cancel();
            Body.OnEndRead(Mutex, e);
        }

        public void ProcessChunkRetPdu(byte[] data, int offset, int length)
        {
            Body.OnDataWrite(Mutex, data ?? new byte[0], offset, length);
        }

        private void OnBodyChunkReadCallback(int bytesToRead)
        {
            SendChunkGetPdu(bytesToRead);
        }

        private void OnBodyEndReadCallback()
        {
            if (ChunkGetPduType == TransferPdu.PduTypeResponseChunkGet)
            {
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
                Mutex.RunExclusively(_ =>
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
    }
}
