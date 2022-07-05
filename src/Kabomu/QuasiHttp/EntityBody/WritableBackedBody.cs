using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class WritableBackedBody : IQuasiHttpBody
    {
        private readonly LinkedList<ReadWriteRequest> _writeRequests;
        private ReadWriteRequest _readRequest;
        private bool _endOfReadSeen, _endOfWriteSeen;

        public WritableBackedBody(string contentType)
        {
            ContentType = contentType;
            _writeRequests = new LinkedList<ReadWriteRequest>();
            MutexApi = new LockBasedMutexApi(new object());
        }

        public long ContentLength => -1;

        public string ContentType { get; }

        public IMutexApi MutexApi { get; set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task<int> readTask;
            using (await MutexApi.Synchronize())
            {
                EntityBodyUtilsInternal.TryCancelRead(_endOfReadSeen);

                if (_readRequest != null)
                {
                    throw new Exception("pending read exists");
                }
                // respond immediately if writes have ended, or if any zero-byte read request is seen.
                if (_endOfWriteSeen || bytesToRead == 0)
                {
                    return 0;
                }
                _readRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = bytesToRead,
                    ReadCallback = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)
                };
                readTask = _readRequest.ReadCallback.Task;

                if (_writeRequests.Count > 0)
                {
                    MatchPendingWriteAndRead();
                }
            }

            return await readTask;
        }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            return WritePossiblyLastBytes(false, data, offset, length);
        }

        public Task WriteLastBytes(byte[] data, int offset, int length)
        {
            return WritePossiblyLastBytes(true, data, offset, length);
        }

        private async Task WritePossiblyLastBytes(bool isLastBytes, byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid source buffer");
            }

            Task writeTask;
            using (await MutexApi.Synchronize())
            {
                EntityBodyUtilsInternal.TryCancelRead(_endOfReadSeen);

                if (_endOfWriteSeen)
                {
                    throw new Exception("end of write");
                }
                // respond immediately to any zero-byte write except if it is a last write.
                if (length == 0 && !isLastBytes)
                {
                    return;
                }
                var writeRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    IsLastWrite = isLastBytes,
                    WriteCallback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _writeRequests.AddLast(writeRequest);
                writeTask = writeRequest.WriteCallback.Task;

                if (_readRequest != null)
                {
                    MatchPendingWriteAndRead();
                }
            }

            await writeTask;
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
            // (not really necessary for promise-based implementations).
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
            pendingRead.ReadCallback.SetResult(bytesToReturn);
            if (pendingWrite != null)
            {
                pendingWrite.WriteCallback.SetResult(true);
            }
            if (writesToFail != null)
            {
                var writeError = new Exception("end of write");
                foreach (var writeReq in writesToFail)
                {
                    writeReq.WriteCallback.SetException(writeError);
                }
            }
        }

        public async Task EndRead()
        {
            using (await MutexApi.Synchronize())
            {
                if (_endOfReadSeen)
                {
                    return;
                }
                _endOfReadSeen = true;
                var srcEndError = EntityBodyUtilsInternal.ReadCancellationException;
                _readRequest?.ReadCallback.SetException(srcEndError);
                foreach (var writeReq in _writeRequests)
                {
                    writeReq.WriteCallback.SetException(srcEndError);
                }
                _readRequest = null;
                _writeRequests.Clear();
            }
        }

        class ReadWriteRequest
        {
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public bool IsLastWrite { get; set; }
            public TaskCompletionSource<bool> WriteCallback { get; set; }
            public TaskCompletionSource<int> ReadCallback { get; set; }
        }
    }
}
