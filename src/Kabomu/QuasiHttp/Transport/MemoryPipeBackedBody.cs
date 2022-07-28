using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// <para>
    /// Implementation of quasi http body which is based on a "pipe" of bytes, where one thread writes data to one end of it,
    /// and another thread reads data from the other end of it. The end of the pipe from which data is read serves
    /// as the byte stream to be read by clients.
    /// </para>
    /// <para>
    /// This notion of pipe is purely implemented in memory with locks, and is similar to (but not based on)
    /// OS named pipes, OS anonymous pipes and OS shell pipes.
    /// </para>
    /// </summary>
    public class MemoryPipeBackedBody : IQuasiHttpBody
    {
        private readonly LinkedList<ReadRequest> _readRequests;
        private readonly LinkedList<WriteRequest> _writeRequests;
        private bool _endOfWriteSeen;
        private Exception _endOfReadError;

        public MemoryPipeBackedBody()
        {
            _readRequests = new LinkedList<ReadRequest>();
            _writeRequests = new LinkedList<WriteRequest>();
            MutexApi = new LockBasedMutexApi();
        }

        public IMutexApi MutexApi { get; set; }
        public long ContentLength => -1;
        public string ContentType { get; set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task<int> readTask;
            using (await MutexApi.Synchronize())
            {
                if (_endOfReadError != null)
                {
                    throw _endOfReadError;
                }

                // respond immediately if writes have ended, or if any zero-byte read request is seen.
                if (_endOfWriteSeen || bytesToRead == 0)
                {
                    return 0;
                }
                var readRequest = new ReadRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = bytesToRead,
                    ReadCallback = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _readRequests.AddLast(readRequest);
                readTask = readRequest.ReadCallback.Task;

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
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid source buffer");
            }

            Task writeTask;
            using (await MutexApi.Synchronize())
            {
                if (_endOfReadError != null)
                {
                    throw _endOfReadError;
                }

                if (_endOfWriteSeen)
                {
                    throw new Exception("end of write");
                }
                // respond immediately to any zero-byte write except if it is a last write.
                if (length == 0 && !isLastBytes)
                {
                    return;
                }
                var writeRequest = new WriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    IsLastWrite = isLastBytes,
                    WriteCallback = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _writeRequests.AddLast(writeRequest);
                writeTask = writeRequest.WriteCallback.Task;

                if (_readRequests.Count > 0)
                {
                    MatchPendingWriteAndRead();
                }
            }

            await writeTask;
        }

        private void MatchPendingWriteAndRead()
        {
            var pendingRead = _readRequests.First.Value;
            var pendingWrite = _writeRequests.First.Value;
            var bytesToReturn = Math.Min(pendingWrite.Length, pendingRead.Length);
            Array.Copy(pendingWrite.Data, pendingWrite.Offset,
                pendingRead.Data, pendingRead.Offset, bytesToReturn);

            // do not invoke callbacks until state of this body is updated,
            // to prevent error of re-entrant read byte requests
            // matching previous writes.
            // NB: not really necessary for promise-based implementations.
            // in fact as a historical note, problems with re-entrancy and
            // excessive stack size growth during looping callbacks, sped up work to 
            // promisify the entire Kabomu library.
            _readRequests.RemoveFirst();
            List<ReadRequest> readsToEnd = null;
            List<WriteRequest> writesToFail = null;
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
                    readsToEnd = new List<ReadRequest>(_readRequests);
                    _readRequests.Clear();
                    writesToFail = new List<WriteRequest>(_writeRequests);
                    _writeRequests.Clear();
                }
            }

            // now we can invoke callbacks.
            // but depend only on local variables due to re-entrancy
            // reason stated above.
            InvokeCallbacksForPendingWriteAndRead(bytesToReturn, pendingRead, pendingWrite, readsToEnd, writesToFail);
        }

        private static void InvokeCallbacksForPendingWriteAndRead(int bytesToReturn, 
            ReadRequest pendingRead, WriteRequest pendingWrite,
            List<ReadRequest> readsToEnd, List<WriteRequest> writesToFail)
        {
            pendingRead.ReadCallback.SetResult(bytesToReturn);
            if (pendingWrite != null)
            {
                pendingWrite.WriteCallback.SetResult(null);
            }
            if (readsToEnd != null)
            {
                foreach (var readReq in readsToEnd)
                {
                    readReq.ReadCallback.SetResult(0);
                }
            }
            if (writesToFail != null)
            {
                var writeError = new EndOfWriteException("end of write");
                foreach (var writeReq in writesToFail)
                {
                    writeReq.WriteCallback.SetException(writeError);
                }
            }
        }

        public Task EndRead()
        {
            return EndRead(null);
        }

        public async Task EndRead(Exception cause)
        {
            using (await MutexApi.Synchronize())
            {
                if (_endOfReadError != null)
                {
                    return;
                }
                _endOfReadError = cause ?? new EndOfReadException();
                foreach (var readReq in _readRequests)
                {
                    readReq.ReadCallback.SetException(_endOfReadError);
                }
                foreach (var writeReq in _writeRequests)
                {
                    writeReq.WriteCallback.SetException(_endOfReadError);
                }
                _readRequests.Clear();
                _writeRequests.Clear();
            }
        }

        class ReadRequest
        {
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public TaskCompletionSource<int> ReadCallback { get; set; }
        }

        class WriteRequest
        {
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public bool IsLastWrite { get; set; }
            public TaskCompletionSource<object> WriteCallback { get; set; }
        }
    }
}
