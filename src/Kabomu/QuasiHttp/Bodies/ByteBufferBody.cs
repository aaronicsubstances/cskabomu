using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Bodies
{
    public class ByteBufferBody : IQuasiHttpBody
    {
        private Exception _srcEndError;

        public ByteBufferBody(byte[] data, int offset, int length, string contentType, IMutexApi mutexApi)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
            }

            Buffer = data;
            Offset = offset;
            ContentLength = length;
            ContentType = contentType;
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }

        public byte[] Buffer { get; }
        public int Offset { get; }
        public int ContentLength { get; }
        public string ContentType { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(QuasiHttpBodyCallback cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            MutexApi.PostCallback(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, null, 0, 0, false);
                    return;
                }
                cb.Invoke(null, Buffer, Offset, ContentLength, false);
            }, null);
        }

        public void Close()
        {
            MutexApi.PostCallback(_ =>
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
