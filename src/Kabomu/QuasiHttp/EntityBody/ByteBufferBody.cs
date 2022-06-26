using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ByteBufferBody : IQuasiHttpBody
    {
        private readonly object _lock = new object();

        private int _bytesRead;
        private Exception _srcEndError;

        public ByteBufferBody(byte[] data, int offset, int length, string contentType)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid source buffer");
            }

            Buffer = data;
            Offset = offset;
            Length = length;
            ContentType = contentType ?? TransportUtils.ContentTypeByteStream;
        }

        public byte[] Buffer { get; }
        public int Offset { get; }
        public int Length { get; }
        public long ContentLength => Length;
        public string ContentType { get; }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            lock (_lock)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                var lengthToUse = Math.Min(Length - _bytesRead, length);
                Array.Copy(Buffer, Offset + _bytesRead, data, offset, lengthToUse);
                _bytesRead += lengthToUse;
                return lengthToUse;
            }
        }

        public async Task EndRead(Exception e)
        {
            lock (_lock)
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = e ?? new Exception("end of read");
            }
        }
    }
}
