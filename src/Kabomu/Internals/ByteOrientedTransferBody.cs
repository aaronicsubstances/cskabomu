using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Internals
{
    internal class ByteOrientedTransferBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly Action _closeCallback;
        private int _readContentLength;
        private Exception _srcEndError;

        public ByteOrientedTransferBody(int contentLength, string contentType,
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

        public string ContentType { get; }
        public int ContentLength { get; }

        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
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
                _transport.ReadBytes(_connection, data, offset, bytesToRead, (e, length) =>
                {
                    mutex.RunExclusively(_ =>
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
                        if (length < 0)
                        {
                            EndRead(cb, new Exception("invalid negative size received"));
                            return;
                        }
                        if (length > bytesToRead)
                        {
                            EndRead(cb, new Exception("received bytes more than requested size"));
                            return;
                        }
                        if (ContentLength >= 0)
                        {
                            if (length == 0 && _readContentLength != ContentLength)
                            {
                                EndRead(cb, new Exception("content length not achieved"));
                                return;
                            }
                            if (_readContentLength + length > ContentLength)
                            {
                                EndRead(cb, new Exception("content length exceeded"));
                                return;
                            }
                            _readContentLength += length;
                        }
                        cb.Invoke(null, length);
                    }, null);
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
