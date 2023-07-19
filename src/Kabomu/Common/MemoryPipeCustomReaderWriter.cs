using System;
using System.Collections.Generic;
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
        private readonly ICustomDisposable _dependent;
        private ReadWriteRequest _readRequest, _writeRequest;
        private bool _disposed;
        private Exception _endOfReadError, _endOfWriteError;

        public MemoryPipeCustomReaderWriter()
            : this(null)
        {

        }

        public MemoryPipeCustomReaderWriter(ICustomDisposable dependent)
        {
            _dependent = dependent;
        }

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

                // respond immediately to any zero-byte read
                if (length == 0)
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

        public async Task DeferCustomDispose(Func<Task> dependentTask)
        {
            try
            {
                if (dependentTask != null)
                {
                    await dependentTask.Invoke();
                }
                await CustomDispose();
            }
            catch (Exception e)
            {
                await CustomDispose(e);
            }
        }

        public Task CustomDispose()
        {
            return CustomDispose(null);
        }

        public async Task CustomDispose(Exception e)
        {
            lock (_mutex)
            {
                if (_disposed)
                {
                    return;
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
            if (_dependent != null)
            {
                await _dependent.CustomDispose();
            }
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