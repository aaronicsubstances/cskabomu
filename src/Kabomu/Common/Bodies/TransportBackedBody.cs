using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class TransportBackedBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private long _contentLength;
        private long _bytesRemaining;
        private Exception _srcEndError;

        public TransportBackedBody(IQuasiHttpTransport transport, object connection)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            _transport = transport;
            _connection = connection;
        }

        public long ContentLength
        {
            get
            {
                return _contentLength;
            }
            set
            {
                _contentLength = value;
                _bytesRemaining = -1;
                if (_contentLength >= 0)
                {
                    _bytesRemaining = _contentLength;
                }
            }
        }

        public string ContentType { get; internal set; }

        internal Func<Task> CloseCallback { get; set; }

        public async Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task<int> readTask;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                if (bytesToRead == 0 || _bytesRemaining == 0)
                {
                    return 0;
                }
                if (_bytesRemaining > 0)
                {
                    bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
                }
                readTask = _transport.ReadBytes(_connection, data, offset, bytesToRead);
            }

            int bytesRead = await readTask;

            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                if (_bytesRemaining > 0)
                {
                    if (bytesRead == 0)
                    {
                        var e = new Exception($"could not read remaining {_bytesRemaining} " +
                            $"bytes before end of read");
                        throw e;
                    }
                    _bytesRemaining -= bytesRead;
                }
                return bytesRead;
            }
        }

        public async Task EndRead(IEventLoopApi eventLoop, Exception e)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }

            Task closeCbTask = null;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    return;
                }

                _srcEndError = e ?? new Exception("end of read");
                if (CloseCallback != null)
                {
                    closeCbTask = CloseCallback.Invoke();
                }
            }

            if (closeCbTask != null)
            {
                await closeCbTask;
            }
        }
    }
}
