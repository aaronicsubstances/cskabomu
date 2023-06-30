using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ByteBufferCustomWritable : ICustomReader, ICustomWritable
    {
        private int _bytesRead = 0;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="data">backing byte array</param>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null</exception>
        public ByteBufferCustomWritable(byte[] data) :
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
        public ByteBufferCustomWritable(byte[] data, int offset, int length)
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

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            length = Math.Max(0, Math.Min(Length - _bytesRead, length));
            Array.Copy(Buffer, Offset + _bytesRead, data, offset, length);
            _bytesRead += length;
            return Task.FromResult(length);
        }

        public Task CustomDispose()
        {
            return Task.CompletedTask;
        }

        public Task WriteBytesTo(ICustomWriter writer)
        {
            return writer.WriteBytes(Buffer, Offset, Length);
        }
    }
}
