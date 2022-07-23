using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ByteBufferBody : IQuasiHttpBody
    {
        private readonly CancellationTokenSource _readCancellationHandle = new CancellationTokenSource();
        private int _bytesRead;

        public ByteBufferBody(byte[] data):
            this(data, 0, data?.Length ?? 0)
        {
        }

        public ByteBufferBody(byte[] data, int offset, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid source buffer");
            }

            Buffer = data;
            Offset = offset;
            Length = length;
        }

        public byte[] Buffer { get; }
        public int Offset { get; }
        public int Length { get; }
        public long ContentLength => Length;
        public string ContentType { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            var lengthToUse = Math.Min(Length - _bytesRead, length);
            Array.Copy(Buffer, Offset + _bytesRead, data, offset, lengthToUse);
            _bytesRead += lengthToUse;
            return Task.FromResult(lengthToUse);
        }

        public Task EndRead()
        {
            _readCancellationHandle.Cancel();
            return Task.CompletedTask;
        }
    }
}
