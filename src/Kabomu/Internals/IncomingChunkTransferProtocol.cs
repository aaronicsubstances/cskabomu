using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Internals
{
    internal class IncomingChunkTransferProtocol
    {
        private STCancellationIndicator _chunkProcessingCancellationIndicator;
        private int _expectedSeqNr = 1;

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
            _chunkProcessingCancellationIndicator?.Cancel();
            Body.OnEndRead(Mutex, e);
        }

        public void ProcessChunkRetPdu(int seqNr, byte[] data, int offset, int length)
        {
            if (_expectedSeqNr != seqNr)
            {
                // ignore duplicates.
                return;
            }
            // cancel the most recent sending of ChunkGet if it has not yet returned.
            _chunkProcessingCancellationIndicator?.Cancel();
            // prevent processing of duplicates
            _expectedSeqNr++;
            Body.OnDataWrite(Mutex, data, offset, length);
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
                SequenceNumber = _expectedSeqNr,
                ContentLength = bytesToRead
            };
            var cancellationIndicator = new STCancellationIndicator();
            _chunkProcessingCancellationIndicator = cancellationIndicator;
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
            byte[] pduBytes = pdu.Serialize(false);
            Transport.SendMessage(Connection, pduBytes, 0, pduBytes.Length,
                ProtocolUtils.CreateCancellationEnquirer(Mutex, cancellationIndicator), 
                cb);
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
