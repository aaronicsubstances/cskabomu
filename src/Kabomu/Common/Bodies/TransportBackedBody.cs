using System;
using System.Collections.Generic;
using System.Text;

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

        internal Action CloseCallback { get; set; }

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, 
            Action<Exception, int> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                if (_bytesRemaining == 0)
                {
                    cb.Invoke(null, 0);
                    return;
                }
                if (_bytesRemaining > 0)
                {
                    bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
                }
                _transport.ReadBytes(_connection, data, offset, bytesToRead, (e, bytesRead) =>
                {
                    if (_srcEndError != null)
                    {
                        cb.Invoke(_srcEndError, 0);
                        return;
                    }
                    if (e != null)
                    {
                        EndRead(cb, e);
                        return;
                    }
                    if (_bytesRemaining > 0)
                    {
                        if (bytesRead == 0)
                        {
                            EndRead(cb, new Exception($"could not read remaining {_bytesRemaining} " +
                                $"bytes before end of read"));
                            return;
                        }
                        _bytesRemaining -= bytesRead;
                    }
                    cb.Invoke(null, bytesRead);
                });
            }, null);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                EndRead(null, e);
            }, null);
        }

        private void EndRead(Action<Exception, int> cb, Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            cb?.Invoke(_srcEndError, 0);
            CloseCallback?.Invoke();
        }
    }
}
