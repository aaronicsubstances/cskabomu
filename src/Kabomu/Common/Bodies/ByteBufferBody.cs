using System;
using System.Collections.Generic;
using System.Text;

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

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            if (mutex == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
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
                var lengthToUse = Math.Min(Length - _bytesRead, length);
                Array.Copy(Buffer, Offset + _bytesRead, data, offset, lengthToUse);
                _bytesRead += lengthToUse;
                cb.Invoke(null, lengthToUse);
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
            }, null);
        }
    }
}
