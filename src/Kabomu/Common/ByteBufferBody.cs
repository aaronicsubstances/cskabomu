using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class ByteBufferBody : IQuasiHttpBody
    {
        private int _bytesRead;
        private Exception _srcEndError;

        public ByteBufferBody(byte[] data, int offset, int length, string contentType)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid buffer");
            }

            Buffer = data;
            Offset = offset;
            ContentLength = length;
            ContentType = contentType ?? "application/octet-stream";
        }

        public byte[] Buffer { get; }
        public int Offset { get; }
        public int ContentLength { get; }
        public string ContentType { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (bytesToRead < 0)
            {
                throw new ArgumentException("received negative bytes to read");
            }
            mutex.RunExclusively(_ =>
            {
                if (_srcEndError != null)
                {
                    cb.Invoke(_srcEndError, 0);
                    return;
                }
                var lengthToUse = Math.Min(ContentLength - _bytesRead, bytesToRead);
                Array.Copy(Buffer, Offset, data, offset, lengthToUse);
                _bytesRead += lengthToUse;
                cb.Invoke(null, lengthToUse);
            }, null);
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
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
