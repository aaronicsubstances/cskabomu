using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
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
    public class MemoryPipeCustomReaderWriter : ICustomReader, ICustomWriter
    {
        private readonly object _mutex = new object();
        private readonly ICustomDisposable _dependent;
        private ReadWriteRequest _readRequest, _writeRequest;
        private bool _endOfReadSeen, _endOfWriteSeen;
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
                if (_endOfWriteSeen)
                {
                    if (_endOfWriteError != null)
                    {
                        throw _endOfWriteError;
                    }
                    return 0;
                }

                // respond immediately to any zero-byte write
                if (length == 0)
                {
                    return 0;
                }

                if (_readRequest != null)
                {
                    throw new InvalidOperationException("pending read exist");
                }
                _readRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    ReadCallback = new TaskCompletionSource<int>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                readTask = _readRequest.ReadCallback.Task;

                if (_writeRequest != null)
                {
                    MatchPendingWriteAndRead();
                }
            }

            return await readTask;
        }

        public Task EndRead()
        {
            return EndRead(null);
        }

        public Task EndRead(Exception e)
        {
            lock (_mutex)
            {
                _endOfReadSeen = true;
                _endOfReadError = e ?? new EndOfWriteException();
                if (_writeRequest != null && _readRequest == null)
                {
                    _writeRequest.WriteCallback.SetException(_endOfReadError);
                    _writeRequest = null;
                }
            }

            return Task.CompletedTask;
        }

        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            Task writeTask;
            lock (_mutex)
            {
                if (_endOfReadSeen)
                {
                    throw _endOfReadError;
                }

                // respond immediately to any zero-byte write
                if (length == 0)
                {
                    return;
                }

                if (_writeRequest != null)
                {
                    throw new InvalidOperationException("pending write exist");
                }
                _writeRequest = new ReadWriteRequest
                {
                    Data = data,
                    Offset = offset,
                    Length = length,
                    WriteCallback = new TaskCompletionSource<object>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };

                writeTask = _writeRequest.WriteCallback.Task;

                if (_readRequest != null)
                {
                    MatchPendingWriteAndRead();
                }
            }

            await writeTask;
        }

        public Task EndWrite()
        {
            return EndWrite(null);
        }

        public Task EndWrite(Exception e)
        {
            lock (_mutex)
            {
                if (!_endOfWriteSeen)
                {
                    _endOfWriteSeen = true;
                    _endOfWriteError = e; // null allowed
                    if (_readRequest != null && _writeRequest == null)
                    {
                        if (_endOfWriteError != null)
                        {
                            _readRequest.ReadCallback.SetException(_endOfWriteError);
                        }
                        else
                        {
                            _readRequest.ReadCallback.SetResult(0);
                        }
                        _readRequest = null;
                    }
                }
            }

            return Task.CompletedTask;
        }

        private void MatchPendingWriteAndRead()
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
                _writeRequest.WriteCallback.SetResult(null);
                _writeRequest = null;
            }

            _readRequest.ReadCallback.SetResult(bytesToReturn);
            _readRequest = null;
        }

        public Task CustomDispose()
        {
            return _dependent?.CustomDispose() ?? Task.CompletedTask;
        }

        class ReadWriteRequest
        {
            public byte[] Data { get; set; }
            public int Offset { get; set; }
            public int Length { get; set; }
            public TaskCompletionSource<int> ReadCallback { get; set; }
            public TaskCompletionSource<object> WriteCallback { get; set; }
        }
    }
}