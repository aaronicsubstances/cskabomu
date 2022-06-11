using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Bodies
{
    public class WritableBackedBody : IQuasiHttpBody
    {
        private readonly LinkedList<ReadWriteRequest> _writeRequests;
        private ReadWriteRequest _readRequest;
        private bool _endOfWriteSeen;
        private Exception _srcEndError;

        public WritableBackedBody(string contentType)
        {
            ContentType = contentType;
            _writeRequests = new LinkedList<ReadWriteRequest>();
        }

        public string ContentType { get; }

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                if (_readRequest != null)
                {
                    cb.Invoke(new Exception("outstanding read exists"), 0);
                    return;
                }
                // respond immediately if writes have ended, or if any zero-byte read request is seen.
                if (_endOfWriteSeen || bytesToRead == 0)
                {
                    cb.Invoke(null, 0);
                    return;
                }
                _readRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = bytesToRead,
                    ReadCallback = cb
                };

                if (_writeRequests.Count > 0)
                {
                    MatchPendingWriteAndRead();
                }
            }, null);
        }

        public void WriteBytes(IMutexApi mutex, byte[] data, int offset, int length, Action<Exception> cb)
        {
            WritePossiblyLastBytes(mutex, false, data, offset, length, cb);
        }

        public void WriteLastBytes(IMutexApi mutex, byte[] data, int offset, int length, Action<Exception> cb)
        {
            WritePossiblyLastBytes(mutex, true, data, offset, length, cb);
        }

        private void WritePossiblyLastBytes(IMutexApi mutex, bool isLastBytes, byte[] data, int offset, int length, Action<Exception> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid source buffer");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError);
                    return;
                }
                if (_endOfWriteSeen)
                {
                    cb.Invoke(new Exception("end of write"));
                    return;
                }
                // respond immediately to any zero-byte write except if it is a last write.
                if (length == 0 && !isLastBytes)
                {
                    cb.Invoke(null);
                    return;
                }
                var writeRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    IsLastWrite = isLastBytes,
                    WriteCallback = cb
                };
                _writeRequests.AddLast(writeRequest);

                if (_readRequest == null)
                {
                    return;
                }

                MatchPendingWriteAndRead();
            }, null);
        }

        private void MatchPendingWriteAndRead()
        {
            var pendingRead = _readRequest;
            var pendingWrite = _writeRequests.First.Value;
            var bytesToReturn = Math.Min(pendingWrite.Length, pendingRead.Length);
            Array.Copy(pendingWrite.Data, pendingWrite.Offset,
                pendingRead.Data, pendingRead.Offset, bytesToReturn);

            // do not invoke callbacks until state of this body is updated,
            // to prevent error of re-entrant read byte requests
            // matching previous writes.
            _readRequest = null;
            List<ReadWriteRequest> writesToFail = null;
            if (bytesToReturn < pendingWrite.Length)
            {
                pendingWrite.Offset += bytesToReturn;
                pendingWrite.Length -= bytesToReturn;
                pendingWrite = null;
            }
            else
            {
                _writeRequests.RemoveFirst();
                if (pendingWrite.IsLastWrite)
                {
                    _endOfWriteSeen = true;
                    writesToFail = new List<ReadWriteRequest>(_writeRequests);
                    _writeRequests.Clear();
                }
            }

            // now we can invoke callbacks.
            // but depend only on local variables due to re-entrancy
            // reason stated above.
            InvokeCallbacksForPendingWriteAndRead(bytesToReturn, pendingRead, pendingWrite, writesToFail);
        }

        private static void InvokeCallbacksForPendingWriteAndRead(int bytesToReturn, 
            ReadWriteRequest pendingRead, ReadWriteRequest pendingWrite, List<ReadWriteRequest> writesToFail)
        {
            pendingRead.ReadCallback.Invoke(null, bytesToReturn);
            if (pendingWrite == null)
            {
                return;
            }
            pendingWrite.WriteCallback.Invoke(null);
            if (writesToFail != null)
            {
                var writeError = new Exception("end of write");
                foreach (var writeReq in writesToFail)
                {
                    writeReq.WriteCallback.Invoke(writeError);
                }
            }
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = e ?? new Exception("end of read");
                _readRequest?.ReadCallback.Invoke(_srcEndError, 0);
                foreach (var writeReq in _writeRequests)
                {
                    writeReq.WriteCallback.Invoke(_srcEndError);
                }
                _readRequest = null;
                _writeRequests.Clear();
            }, null);
        }

        private class ReadWriteRequest
        {
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public bool IsLastWrite { get; set; }
            public Action<Exception> WriteCallback { get; set; }
            public Action<Exception, int> ReadCallback { get; set; }
        }
    }
}
