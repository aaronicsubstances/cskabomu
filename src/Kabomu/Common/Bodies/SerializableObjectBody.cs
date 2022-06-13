using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common.Bodies
{
    public class SerializableObjectBody : IQuasiHttpBody
    {
        private IQuasiHttpBody _byteBufferBody;
        private Exception _srcEndError;

        public SerializableObjectBody(object content, Func<object, byte[]> serializationHandler, string contentType)
        {
            if (content == null)
            {
                throw new ArgumentException("null content");
            }
            if (serializationHandler == null)
            {
                throw new ArgumentException("null serialization handler");
            }
            Content = content;
            SerializationHandler = serializationHandler;
            ContentType = contentType ?? TransportUtils.ContentTypeJson;
        }

        public object Content { get; }

        public long ContentLength => -1;

        public string ContentType { get; }

        public Func<object, byte[]> SerializationHandler { get; }

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
                if (_byteBufferBody == null)
                {
                    try
                    {
                        var srcData = SerializationHandler.Invoke(Content);
                        _byteBufferBody = new ByteBufferBody(srcData, 0, srcData.Length, null);
                    }
                    catch (Exception e)
                    {
                        EndRead(mutex, cb, e);
                        return;
                    }
                }
                _byteBufferBody.ReadBytes(mutex, data, offset, bytesToRead, cb);
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
                EndRead(mutex, null, e);
            }, null);
        }

        private void EndRead(IMutexApi mutex, Action<Exception, int> cb, Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            _byteBufferBody?.OnEndRead(mutex, e);
            cb?.Invoke(_srcEndError, 0);
        }
    }
}
