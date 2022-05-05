using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Bodies
{
    public class StringBody : IQuasiHttpBody
    {
        private byte[] _buffer;
        private Exception _srcEndError;

        public StringBody(string content, string contentType, IMutexApi mutexApi)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            ContentType = contentType ?? "text/plain";
            ContentLength = Encoding.UTF8.GetByteCount(content);
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }

        public string Content { get; }

        public string ContentType { get; }

        public int ContentLength { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(QuasiHttpBodyCallback cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, null, 0, 0);
                    return;
                }
                int lengthRemaining = 0;
                if (_buffer == null)
                {
                    _buffer = Encoding.UTF8.GetBytes(Content);
                    lengthRemaining = _buffer.Length;
                }
                cb.Invoke(null, _buffer, 0, lengthRemaining);
            }, null);
        }

        public void OnEndRead(Exception e)
        {
            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = e ?? new Exception("end of read");
            }, null);
        }
    }
}
