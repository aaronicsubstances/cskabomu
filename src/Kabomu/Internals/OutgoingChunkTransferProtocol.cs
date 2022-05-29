using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Kabomu.Internals
{
    internal class OutgoingChunkTransferProtocol
    {
        private STCancellationIndicator _chunkProcessingCancellationIndicator;
        private int _expectedSeqNr = 1;

        public OutgoingChunkTransferProtocol(IQuasiHttpTransport transport, IMutexApi mutex,
            object connection, Action<Exception> abortCallback, byte chunkRetPduType,
            IQuasiHttpBody body)
        {
            Transport = transport;
            Mutex = mutex;
            Connection = connection;
            AbortCallback = abortCallback;
            ChunkRetPduType = chunkRetPduType;
            Body = body;
        }

        public IQuasiHttpTransport Transport { get; }
        public IMutexApi Mutex { get; }
        public object Connection { get; }
        public Action<Exception> AbortCallback { get; }
        public byte ChunkRetPduType { get; }
        public IQuasiHttpBody Body { get; }

        public void Cancel(Exception e)
        {
            _chunkProcessingCancellationIndicator?.Cancel();
            Body.OnEndRead(Mutex, e);
        }

        public void ProcessChunkGetPdu(int seqNr, int bytesToRead)
        {
            if (_expectedSeqNr != seqNr)
            {
                // ignore duplicates.
                return;
            }
            // cancel the most recent sending of ChunkRet if it has not yet returned.
            _chunkProcessingCancellationIndicator?.Cancel();
            // prevent processing of duplicates
            _expectedSeqNr++;
            var cancellationIndicator = new STCancellationIndicator();
            _chunkProcessingCancellationIndicator = cancellationIndicator;
            byte[] data = new byte[bytesToRead];
            Action<Exception, int> cb = (e, bytesRead) =>
            {
                if (!cancellationIndicator.Cancelled)
                {
                    cancellationIndicator.Cancel();
                    HandleBodyChunkReadOutcome(e, data, 0, bytesRead);
                }
            };
            Body.OnDataRead(Mutex, data, 0, data.Length, cb);
        }

        private void HandleBodyChunkReadOutcome(Exception e, byte[] data, int offset, int length)
        {
            if (e != null)
            {
                AbortCallback.Invoke(e);
                return;
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                AbortCallback.Invoke(new Exception("invalid outgoing body chunk"));
                return;
            }
            SendChunkRetPdu(data, offset, length);
        }

        private void SendChunkRetPdu(byte[] data, int offset, int length)
        {
            var pdu = new TransferPdu
            {
                Version = TransferPdu.Version01,
                PduType = ChunkRetPduType,
                SequenceNumber = _expectedSeqNr - 1,
                Data = data,
                DataOffset = offset,
                DataLength = length
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
