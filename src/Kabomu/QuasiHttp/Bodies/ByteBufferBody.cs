using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Bodies
{
    public class ByteBufferBody : IQuasiHttpBody
    {
        private Exception _srcEndError;

        public ByteBufferBody(byte[] data)
            : this(data, 0, data?.Length ?? 0, "application/octet-stream")
        { }

        public ByteBufferBody(byte[] data, int offset, int length, string contentType)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
            }
            Buffer = data;
            Offset = offset;
            ContentLength = length;
            ContentType = contentType;
        }

        public byte[] Buffer { get; }

        public int Offset { get; }

        public string ContentType { get; }

        public int ContentLength { get; }

        public void OnDataRead(QuasiHttpBodyCallback cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (_srcEndError != null)
            {
                cb.Invoke(_srcEndError, null, 0, 0, false);
                return;
            }
            cb.Invoke(null, Buffer, Offset, ContentLength, false);
        }

        public void Close()
        {
            if (_srcEndError != null)
            {
                return;
            }
            _srcEndError = new Exception("closed");
        }
    }
}
