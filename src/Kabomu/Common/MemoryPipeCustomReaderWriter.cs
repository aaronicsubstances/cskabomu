using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of <see cref="ICustomReader"/> and <see cref="ICustomWriter"/>
    /// interfaces which is based on a "pipe" of bytes, where one thread writes 
    /// data to one end of it,
    /// and another thread reads data from the other end of it.
    /// The end of the pipe from which data is read serves
    /// as the byte stream to be read by clients.
    /// </summary>
    /// <remarks>
    /// This notion of pipe is purely implemented in memory with locks,
    /// and is similar to (but not based on)
    /// OS named pipes, OS anonymous pipes and OS shell pipes.
    /// </remarks>
    public class MemoryPipeCustomReaderWriter : ICustomReader, ICustomWriter
    {
        /// <summary>
        /// The default high water mark. Equal to 8,192 bytes.
        /// </summary>
        private static readonly int DefaultHighWaterMark = 8_192;

        private readonly object _mutex = new object();
        private readonly LinkedList<ReadWriteRequest> _readRequests =
            new LinkedList<ReadWriteRequest>();
        private readonly LinkedList<ReadWriteRequest> _writeRequests =
            new LinkedList<ReadWriteRequest>();
        private readonly int _highWaterMark = 1;
        private bool _endOfReadWrite;
        private Exception _endOfReadError, _endOfWriteError;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public MemoryPipeCustomReaderWriter()
        {
        }

        /// <summary>
        /// Reads bytes from any pending writes or waits until a write comes through. 
        /// If writes have been ended on the instance, then
        /// an error may be thrown; otherwise zero will be returned.
        /// Note that zero-byte reads will also wait for pending writes to become
        /// available.
        /// </summary>
        /// <param name="data">the destination buffer where bytes read will be stored</param>
        /// <param name="offset">starting position in buffer for storing bytes read</param>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>a task whose result will be the number of bytes actually read. Can
        /// be less than the requested number of bytes if the last write had fewer
        /// number of bytes to supply</returns>
        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("arguments don't constitute a valid byte slice");
            }
            Task<int> readTask;
            lock (_mutex)
            {
                // respond immediately if writes have ended
                if (_endOfReadWrite)
                {
                    if (_endOfReadError != null)
                    {
                        throw _endOfReadError;
                    }
                    return 0;
                }

                // wait for writes even if zero bytes were requested.
                var readRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    Callback = new TaskCompletionSource<int>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _readRequests.AddLast(readRequest);
                readTask = readRequest.Callback.Task;
                if (_writeRequests.Count > 0)
                {
                    MatchPendingWriteAndRead();
                }
            }

            return await readTask;
        }

        /// <summary>
        /// Attempts to write bytes by saving for pickups by enough read calls.
        /// An error is thrown if writes have been ended on the instance, or
        /// if write cannot be permitted within high water mark setting
        /// Note that zero-byte writes return immediately if no error occurs.
        /// </summary>
        /// <param name="data">the source buffer of the bytes to be
        /// fetched for writing to this instance</param>
        /// <param name="offset">the starting position in buffer for
        /// fetching the bytes to be written</param>
        /// <param name="length">the number of bytes to write to instance</param>
        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            if (!await TryWriteBytes(data, offset, length))
            {
                throw new CustomIOException("cannot perform further writes " +
                    "due to high water mark setting");
            }
        }

        /// <summary>
        /// Writes bytes by saving for pickups by enough read calls.
        /// An error is thrown if writes have been ended on the instance.
        /// Note that zero-byte writes return immediately if no error occurs.
        /// </summary>
        /// <param name="data">the source buffer of the bytes to be
        /// fetched for writing to this instance</param>
        /// <param name="offset">the starting position in buffer for
        /// fetching the bytes to be written</param>
        /// <param name="length">the number of bytes to write to instance</param>
        /// <returns>a task whose result is true if and only if
        /// write was permitted within high water mark setting</returns>
        private async Task<bool> TryWriteBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("arguments don't constitute a valid byte slice");
            }
            Task writeTask;
            lock (_mutex)
            {
                if (_endOfReadWrite)
                {
                    throw _endOfWriteError;
                }

                // don't store any zero-byte write
                if (length == 0)
                {
                    return true;
                }

                // check for high water mark.
                // this setting should apply to only existing pending writes,
                // so as to ensure that
                // a write can always be attempted the first time,
                // and one doesn't have to worry about high water mark
                // if one is performing serial writes.
                var totalOutstandingWriteBytes = _writeRequests.Sum(
                    x => x.Length);
                if (totalOutstandingWriteBytes >= _highWaterMark)
                {
                    return false;
                }

                var writeRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    Callback = new TaskCompletionSource<int>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _writeRequests.AddLast(writeRequest);
                writeTask = writeRequest.Callback.Task;

                if (_readRequests.Count > 0)
                {
                    MatchPendingWriteAndRead();
                }
            }

            await writeTask;
            return true;
        }

        private void MatchPendingWriteAndRead()
        {
            var pendingRead = _readRequests.First.Value;
            var pendingWrite = _writeRequests.First.Value;
            var bytesToReturn = Math.Min(pendingRead.Length, pendingWrite.Length);
            Array.Copy(pendingWrite.Data, pendingWrite.Offset,
                pendingRead.Data, pendingRead.Offset, bytesToReturn);

            // do not invoke callbacks until state is updated,
            // to prevent error of re-entrant read byte requests
            // matching previous writes.
            // NB: not really necessary for promise-based implementations.
            // in fact as a historical note, problems with re-entrancy and
            // excessive stack size growth during looping callbacks, sped up work to 
            // promisify the entire Kabomu library.
            _readRequests.RemoveFirst();
            if (bytesToReturn < pendingWrite.Length)
            {
                pendingWrite.Offset += bytesToReturn;
                pendingWrite.Length -= bytesToReturn;
            }
            else
            {
                _writeRequests.RemoveFirst();
                pendingWrite.Callback.SetResult(0);
            }
            pendingRead.Callback.SetResult(bytesToReturn);
        }

        /// <summary>
        /// Causes pending and future read and writes to be aborted with a
        /// supplied exception instance (pending and future reads will return 0
        /// if no exception is supplied).
        /// </summary>
        /// <param name="e">exception instance for ending read and writes.
        /// If null, then a default exception will be used for writes, and
        /// reads will simply return 0</param>
        /// <returns>a task representing the asynchronous operation</returns>
        public Task EndWrites(Exception e = null)
        {
            lock (_mutex)
            {
                if (_endOfReadWrite)
                {
                    return Task.CompletedTask;
                }
                _endOfReadWrite = true;
                _endOfReadError = e;
                _endOfWriteError = e ?? new CustomIOException("end of write");
                foreach (var readRequest in _readRequests)
                {
                    if (_endOfReadError != null)
                    {
                        readRequest.Callback.SetException(_endOfReadError);
                    }
                    else
                    {
                        readRequest.Callback.SetResult(0);
                    }
                }
                foreach (var writeRequest in _writeRequests)
                {
                    writeRequest.Callback.SetException(_endOfWriteError);
                }
                _readRequests.Clear();
                _writeRequests.Clear();
            }
            return Task.CompletedTask;
        }

        class ReadWriteRequest
        {
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public TaskCompletionSource<int> Callback { get; set; }
        }
    }
}