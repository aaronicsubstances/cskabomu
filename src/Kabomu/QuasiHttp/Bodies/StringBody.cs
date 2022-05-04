using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Bodies
{
    public class StringBody : IQuasiHttpBody
    {
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
                var buffer = Encoding.UTF8.GetBytes(Content);
                cb.Invoke(null, buffer, 0, buffer.Length);
                OnEndRead(null);
            }, null);
        }

        private void OnEndRead(Exception error)
        {
            if (_srcEndError != null)
            {
                return;
            }
            _srcEndError = error ?? new Exception("end of read");
        }

        public void Close()
        {
            MutexApi.RunCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = new Exception("closed");
            }, null);
        }
    }
}
