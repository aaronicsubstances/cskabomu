using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Implementation of reader and writer interfaces which is based on a "pipe"
    /// of bytes, where one thread writes data to one end of it,
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
        private readonly object _mutex = new object();
        private readonly bool _answerZeroByteReadsFromPipe;
        private ReadWriteRequest _readRequest, _writeRequest;
        private bool _disposed;
        private Exception _endOfReadError, _endOfWriteError;

        /// <summary>
        /// Creates a new instance.
        /// <paramref name="answerZeroByteReadsFromPipe">pass true
        /// if a request to read zero bytes should use or wait on a pending write;
        /// or pass false to immediately return zero which is the default.</paramref>
        /// </summary>
        public MemoryPipeCustomReaderWriter(bool answerZeroByteReadsFromPipe = false)
        {
            _answerZeroByteReadsFromPipe = answerZeroByteReadsFromPipe;
        }

        /// <summary>
        /// Reads bytes from the last write or waits until a write comes through. If 
        /// instance has been disposed, then depending on how the disposal occured
        /// an error will be thrown or just zero will be returned. It is an error
        /// to call this method if a previous call has not returned.
        /// </summary>
        /// <param name="data">the destination buffer where bytes read will be stored</param>
        /// <param name="offset">starting position in buffer for storing bytes read</param>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>a task whose result will be the number of bytes actually read. Can
        /// be less than the requested number of bytes if the last write had fewer
        /// number of bytes to supply</returns>
        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            Task<int> readTask;
            lock (_mutex)
            {
                // respond immediately if writes have ended
                if (_disposed)
                {
                    if (_endOfReadError != null)
                    {
                        throw _endOfReadError;
                    }
                    return 0;
                }

                if (_readRequest != null)
                {
                    throw new CustomIOException("pending read exist");
                }

                // respond immediately to any zero-byte read, unless
                // instance was set up to wait for writes even if
                // zero bytes were requested.
                if (length == 0 && !_answerZeroByteReadsFromPipe)
                {
                    return 0;
                }
                _readRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length
                };

                if (_writeRequest != null)
                {
                    return MatchPendingWriteAndRead();
                }

                _readRequest.Callback = new TaskCompletionSource<int>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                readTask = _readRequest.Callback.Task;
            }

            return await readTask;
        }

        /// <summary>
        /// Writes bytes by saving for pickups by enough read calls. If 
        /// instance has been disposed, then an error will be thrown. It is an error
        /// to call this method if a previous call has not returned.
        /// </summary>
        /// <param name="data">the source buffer of the bytes to be
        /// fetched for writing to this instance</param>
        /// <param name="offset">the starting position in buffer for
        /// fetching the bytes to be written</param>
        /// <param name="length">the number of bytes to write to instance</param>
        /// <returns>a task representing end of the asynchronous operation</returns>
        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            Task writeTask;
            lock (_mutex)
            {
                if (_disposed)
                {
                    throw _endOfWriteError;
                }

                if (_writeRequest != null)
                {
                    throw new CustomIOException("pending write exist");
                }

                // respond immediately to any zero-byte write
                if (length == 0)
                {
                    return;
                }
                _writeRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length
                };

                if (_readRequest != null)
                {
                    MatchPendingWriteAndRead();
                }

                if (_writeRequest == null)
                {
                    return;
                }

                _writeRequest.Callback = new TaskCompletionSource<int>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                writeTask = _writeRequest.Callback.Task;
            }

            await writeTask;
        }

        private int MatchPendingWriteAndRead()
        {
            var bytesToReturn = Math.Min(_readRequest.Length, _writeRequest.Length);
            Array.Copy(_writeRequest.Data, _writeRequest.Offset,
                _readRequest.Data, _readRequest.Offset, bytesToReturn);

            // do not invoke callbacks until state of this body is updated,
            // to prevent error of re-entrant read byte requests
            // matching previous writes.
            // NB: not really necessary for promise-based implementations.
            // in fact as a historical note, problems with re-entrancy and
            // excessive stack size growth during looping callbacks, sped up work to 
            // promisify the entire Kabomu library.
            if (bytesToReturn < _writeRequest.Length)
            {
                _writeRequest.Offset += bytesToReturn;
                _writeRequest.Length -= bytesToReturn;
            }
            else
            {
                if (_writeRequest.Callback != null)
                {
                    _writeRequest.Callback.SetResult(0);
                }
                _writeRequest = null;
            }

            if (_readRequest.Callback != null)
            {
                _readRequest.Callback.SetResult(bytesToReturn);
            }
            _readRequest = null;

            return bytesToReturn;
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
                if (_disposed)
                {
                    return Task.CompletedTask;
                }
                _disposed = true;
                _endOfReadError = e;
                _endOfWriteError = e ?? new CustomIOException("end of write");
                if (_writeRequest != null)
                {
                    _writeRequest.Callback.SetException(_endOfWriteError);
                    _writeRequest = null;
                }
                if (_readRequest != null)
                {
                    if (_endOfReadError != null)
                    {
                        _readRequest.Callback.SetException(_endOfReadError);
                    }
                    else
                    {
                        _readRequest.Callback.SetResult(0);
                    }
                    _readRequest = null;
                }
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