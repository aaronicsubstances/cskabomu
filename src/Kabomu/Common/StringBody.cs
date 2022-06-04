using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class StringBody : IQuasiHttpBody
    {
        private IQuasiHttpBody _byteBufferBody;
        private Exception _srcEndError;

        public StringBody(string content, string contentType)
        {
            if (content == null)
            {
                throw new ArgumentException("null content");
            }
            Content = content;
            ContentType = contentType ?? "text/plain";
        }

        public string Content { get; }

        public string ContentType { get; }

        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead,
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
                    var srcData = Encoding.UTF8.GetBytes(Content);
                    _byteBufferBody = new ByteBufferBody(srcData, 0, srcData.Length, null);
                }
                _byteBufferBody.OnDataRead(mutex, data, offset, bytesToRead, cb);
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
                _srcEndError = e ?? new Exception("end of read");
                _byteBufferBody?.OnEndRead(mutex, e);
            }, null);
        }
    }
}
