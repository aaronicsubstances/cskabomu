using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Quasi http body implementation backed by in-memory byte array.
    /// </summary>
    public class ByteBufferBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private int _bytesRead;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="data">backing byte array</param>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null</exception>
        public ByteBufferBody(byte[] data):
            this(data, 0, data?.Length ?? 0)
        {
        }

        /// <summary>
        /// Creates a new instance with a specified slice of a byte array.
        /// </summary>
        /// <param name="data">backing byte array</param>
        /// <param name="offset">starting offset of data</param>
        /// <param name="length">number of bytes available for reading</param>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null</exception>
        /// <exception cref="ArgumentException">The combination of offset and length arguments generate invalid indices
        /// in data argument</exception>
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
