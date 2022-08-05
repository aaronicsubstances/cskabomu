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
    /// Implementation of quasi http body which is based on a "pipe" of bytes, where one thread writes data to one end of it,
    /// and another thread reads data from the other end of it. The end of the pipe from which data is read serves
    /// as the byte stream to be read by clients.
    /// </summary>
    /// <remarks>
    /// This notion of pipe is purely implemented in memory with locks, and is similar to (but not based on)
    /// OS named pipes, OS anonymous pipes and OS shell pipes.
    /// </remarks>
    public class MemoryPipeBackedBody : IQuasiHttpBody
    {
        private readonly LinkedList<ReadRequest> _readRequests;
        private readonly LinkedList<WriteRequest> _writeRequests;
        private bool _endOfWriteSeen;
        private Exception _endOfReadError;
        private int _pendingWriteByteCount;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public MemoryPipeBackedBody()
        {
            _readRequests = new LinkedList<ReadRequest>();
            _writeRequests = new LinkedList<WriteRequest>();
            MutexApi = new LockBasedMutexApi();
        }

        /// <summary>
        /// Gets or sets mutex api used to guard multithreaded 
        /// access to operations of this class.
        /// </summary>
        /// <remarks> 
        /// An ordinary lock object is the initial value for this property, and so there is no need to modify
        /// this property except for advanced scenarios.
        /// </remarks>
        public IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Returns -1 to indicate unknown length.
        /// </summary>
        public long ContentLength => -1;

        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the maximum write buffer limit. A positive value means that
        /// any attempt to write (excluding last writes) such that the total number of
        /// bytse outstanding tries to exceed that positive value, will result in an instance of the
        /// <see cref="DataBufferLimitExceededException"/> class to be thrown.
        /// <para></para>
        /// By default this property is zero, and so indicates that no maximum
        /// limit will be imposed on outstanding writes.
        /// </summary>
        public int MaxWriteBufferLimit { get; set; }

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

        /// <summary>
        /// Writes bytes to this instance to serve ongoing or future read requests.
        /// </summary>
        /// <param name="data">source byte buffer</param>
        /// <param name="offset">starting position in data</param>
        /// <param name="length">number of bytes to write</param>
        /// <returns>a task representing the asynchronous write operation</returns>
        /// <exception cref="EndOfReadException">reads have ended</exception>
        /// <exception cref="EndOfWriteException">writes have ended</exception>
        public Task WriteBytes(byte[] data, int offset, int length)
        {
            return WritePossiblyLastBytes(false, data, offset, length);
        }

        /// <summary>
        /// Writes the last bytes to this instance to serve ongoing or future read requests. After this
        /// call future write requests will fail, and future read request will return 0.
        /// </summary>
        /// <param name="data">source byte buffer</param>
        /// <param name="offset">starting position in data</param>
        /// <param name="length">number of bytes to write</param>
        /// <returns>a task representing the asynchronous write operation</returns>
        /// <exception cref="EndOfReadException">reads have ended</exception>
        /// <exception cref="EndOfWriteException">writes have ended</exception>
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
                    throw new EndOfWriteException();
                }

                if (!isLastBytes)
                {
                    // respond immediately to any zero-byte write
                    if (length == 0)
                    {
                        return;
                    }

                    // enforce maximum write buffer size limit if non positive.
                    if (MaxWriteBufferLimit > 0)
                    {
                        if (_pendingWriteByteCount + length > MaxWriteBufferLimit)
                        {
                            throw new DataBufferLimitExceededException(MaxWriteBufferLimit,
                                $"maximum write buffer limit of {MaxWriteBufferLimit} bytes exceeded", null);
                        }
                    }
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
                _pendingWriteByteCount += writeRequest.Length;

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
            _pendingWriteByteCount -= bytesToReturn;

            // do not invoke callbacks until state of this body is updated,
            // to prevent error of re-entrant read byte requests
            // matching previous writes.
            // NB: not really necessary for promise-based implementations.
            // in fact as a historical note, problems with re-entrancy and
            // excessive stack size growth during looping callbacks, sped up work to 
            // promisify the entire Kabomu library.
            _readRequests.RemoveFirst();
            List<ReadRequest> readsToEnd = null;
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
                    if (_writeRequests.Count > 0)
                    {
                        throw new InvalidOperationException("expected write requests to be empty at this stage");
                    }
                    _pendingWriteByteCount = 0;
                }
            }

            // now we can invoke callbacks.
            // but depend only on local variables due to re-entrancy
            // reason stated above.
            InvokeCallbacksForPendingWriteAndRead(bytesToReturn, pendingRead, pendingWrite, readsToEnd);
        }

        private static void InvokeCallbacksForPendingWriteAndRead(int bytesToReturn, 
            ReadRequest pendingRead, WriteRequest pendingWrite,
            List<ReadRequest> readsToEnd)
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
        }

        public Task EndRead()
        {
            return EndRead(null);
        }

        /// <summary>
        /// Like EndRead(), but enables a custom end of read error to be supplied.
        /// </summary>
        /// <param name="cause">the custom end of read error or null to make this call behave
        /// just like EndRead()</param>
        /// <returns>a task representing the asynchronous operation</returns>
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
                _pendingWriteByteCount = 0;
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
