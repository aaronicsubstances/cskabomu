using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class TransportBackedBody : IQuasiHttpBody
    {
        private readonly object _lock = new object();

        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly Func<Task> _closeCallback;
        private long _contentLength;
        private long _bytesRemaining;
        private Exception _srcEndError;

        public TransportBackedBody(IQuasiHttpTransport transport, object connection,
             Func<Task> closeCallback, long contentLength, string contentType)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            _transport = transport;
            _connection = connection;
            _closeCallback = closeCallback;
            ContentType = contentType;
            ContentLength = contentLength;
            _bytesRemaining = -1;
            if (_contentLength >= 0)
            {
                _bytesRemaining = contentLength;
            }
        }

        public long ContentLength { get; private set; }

        public string ContentType { get; private set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task<int> readTask;
            lock (_lock)
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

            lock (_lock)
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

        public async Task EndRead(Exception e)
        {
            Task closeCbTask = null;
            lock (_lock)
            {
                if (_srcEndError != null)
                {
                    return;
                }

                _srcEndError = e ?? new Exception("end of read");
                if (_closeCallback != null)
                {
                    closeCbTask = _closeCallback.Invoke();
                }
            }

            if (closeCbTask != null)
            {
                await closeCbTask;
            }
        }
    }
}
