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

        public async Task<int> ReadBytesAsync(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            if (eventLoop.IsMutexRequired(out Task mt)) await mt;

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
            try
            {
                int bytesRead = await eventLoop.MutexWrap(_transport.ReadBytesAsync(_connection, data, offset, bytesToRead));
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
                        await EndReadInternally(e);
                        throw e;
                    }
                    _bytesRemaining -= bytesRead;
                }
                return bytesRead;
            }
            catch (Exception e)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                await EndReadInternally(e);
                throw;
            }
        }

        public async Task EndReadAsync(IEventLoopApi eventLoop, Exception e)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }

            if (eventLoop.IsMutexRequired(out Task mt)) await mt;

            if (_srcEndError != null)
            {
                return;
            }
            await EndReadInternally(e);
        }

        private async Task EndReadInternally(Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            if (CloseCallback != null)
            {
                await CloseCallback.Invoke();
            }
        }
    }
}
