﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents quasi http body based on a byte buffer.
    /// </summary>
    public class ByteBufferBody : AbstractQuasiHttpBody
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="data">backing byte array</param>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null</exception>
        public ByteBufferBody(byte[] data) :
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
        /// Returns a freshly created reader backed by
        /// <see cref="Buffer"/> property.
        /// </summary>
        public override ICustomReader Reader()
        {
            var stream = new MemoryStream(Buffer, Offset, Length);
            return new StreamCustomReaderWriter(stream);
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public override Task CustomDispose() => Task.CompletedTask;

        /// <summary>
        /// Transfers contents of <see cref="Buffer"/> property
        /// to supplied writer.
        /// </summary>
        /// <param name="writer">supplied writer</param>
        public override Task WriteBytesTo(ICustomWriter writer) =>
            writer.WriteBytes(Buffer, Offset, Length);
    }
}
