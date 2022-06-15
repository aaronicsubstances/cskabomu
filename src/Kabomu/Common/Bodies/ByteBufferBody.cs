using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class ByteBufferBody : IQuasiHttpBody
    {
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

        public async Task<int> ReadBytesAsync(IEventLoopApi eventLoop, byte[] data, int offset, int length)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            if (eventLoop.IsMutexRequired(out Task mt)) await mt;

            if (_srcEndError != null)
            {
                throw _srcEndError;
            }
            var lengthToUse = Math.Min(Length - _bytesRead, length);
            Array.Copy(Buffer, Offset + _bytesRead, data, offset, lengthToUse);
            _bytesRead += lengthToUse;
            return lengthToUse;
        }

        public async Task EndReadAsync(IEventLoopApi eventLoop, Exception e)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }

            if (eventLoop.IsMutexRequired(out Task mt)) await mt;

            if (_srcEndError != null)
            {
                return;
            }
            _srcEndError = e ?? new Exception("end of read");
        }
    }
}
