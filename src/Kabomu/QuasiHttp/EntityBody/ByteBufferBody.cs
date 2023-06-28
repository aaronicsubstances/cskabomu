using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents byte stream directly with in-memory byte array.
    /// </summary>
    public class ByteBufferBody : IQuasiHttpBody, IBytesAlreadyReadProviderInternal
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();

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
            ContentLength = length;
        }

        /// <summary>
        /// Gets the source byte buffer of reads from this implementation.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// Gets the starting position in the source byte buffer from which reads will begin.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the total number of bytes to yield to read requests.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Returns the number of bytes to read, or negative value to indicate that all
        /// the number indicated by the Length property should be returned.
        /// </summary>
        public long ContentLength { get; set; }

        public string ContentType { get; set; }

        long IBytesAlreadyReadProviderInternal.BytesAlreadyRead { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            Task<int> ReadBytesInternal(int bytesToRead)
            {
                var bytesAlreadyRead = ((IBytesAlreadyReadProviderInternal)this).BytesAlreadyRead;

                bytesToRead = (int)Math.Min(Length - bytesAlreadyRead, bytesToRead);
                Array.Copy(Buffer, Offset + bytesAlreadyRead,
                    data, offset, bytesToRead);
                return Task.FromResult(bytesToRead);
            }

            return EntityBodyUtilsInternal.PerformGeneralRead(this,
                length, ReadBytesInternal);
        }

        public Task EndRead()
        {
            _readCancellationHandle.Cancel();
            return Task.CompletedTask;
        }
    }
}
