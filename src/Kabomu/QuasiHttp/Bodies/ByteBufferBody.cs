using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Bodies
{
    public class ByteBufferBody : IQuasiHttpBody
    {
        private int _bytesRead;
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
            ContentType = contentType ?? "application/octet-stream";
            MutexApi = mutexApi ?? new BlockingMutexApi(this);
        }

        public byte[] Buffer { get; }
        public int Offset { get; }
        public int ContentLength { get; }
        public string ContentType { get; }
        public IMutexApi MutexApi { get; }

        public void OnDataRead(byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            if (bytesToRead < 0)
            {
                throw new ArgumentException("received negative bytes to read");
            }
            MutexApi.RunCallback(_ =>
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
