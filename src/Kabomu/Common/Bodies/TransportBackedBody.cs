﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Bodies
{
    public class TransportBackedBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly Action _closeCallback;
        private Exception _srcEndError;

        public TransportBackedBody(long contentLength, string contentType,
            IQuasiHttpTransport transport, object connection, Action closeCallback)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            ContentLength = contentLength;
            ContentType = contentType;
            _transport = transport;
            _connection = connection;
            _closeCallback = closeCallback;
        }

        public long ContentLength { get; }

        public string ContentType { get; }

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
            _closeCallback?.Invoke();
        }
    }
}
