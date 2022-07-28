using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// Structure used to encode quasi http bodies. A quasi http body of unknown length is encoded
    /// as a list of zero or more non-empty serialized instances of this class,
    /// followed by another empty serialized instance. All properties in this structure are optional except for Version.
    /// </summary>
    /// <remarks>
    /// This structure is equivalent to a subset of the chunked transfer encoding scheme in HTTP.
    /// In particular it currently does not support extensions or trailing headers; just chunk length
    /// followed by chunk data.
    /// </remarks>
    public class SubsequentChunk
    {
        /// <summary>
        /// Gets or sets the serialization format version.
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Gets or sets the source byte buffer for the chunk structure.
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Gets or sets the starting position in the source byte buffer for the chunk structure.
        /// </summary>
        public int DataOffset { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes in the source byte buffer to use. This
        /// property can set to zero or a negative value to indicate an empty structure,
        /// in which case <see cref="Data"/> and <see cref="DataOffset"/> properties will
        /// not be used at all.
        /// </summary>
        public int DataLength { get; set; }

        /// <summary>
        /// Serializes the structure into bytes. The serialization version format must be set
        /// or else deserialization will fail later on.
        /// </summary>
        /// <returns>serialized chunk as a list of byte buffer slices.</returns>
        public ByteBufferSlice[] Serialize()
        {
            var serialized = new ByteBufferSlice[DataLength > 0 ? 2 : 1];
            var dataPrefix = new byte[] { Version, Flags };
            serialized[0] = new ByteBufferSlice
            {
                Data = dataPrefix,
                Length = dataPrefix.Length
            };
            if (DataLength > 0)
            {
                serialized[1] = new ByteBufferSlice
                {
                    Data = Data,
                    Offset = DataOffset,
                    Length = DataLength
                };
            }
            return serialized;
        }

        /// <summary>
        /// Deserializes the structure from byte buffer. The serialization format version must be present.
        /// </summary>
        /// <param name="data">the source byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to use</param>
        /// <returns>deserialized subsequent chunk structure</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/> arguments 
        /// genereate invalid positions in source byte buffer.</exception>
        /// <exception cref="Exception">The byte buffer slice provided does not represent valid
        /// subsequent chunk structure, or serialization format version is zero, or deserialization failed.</exception>
        public static SubsequentChunk Deserialize(byte[] data, int offset, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid payload");
            }

            if (length < 2)
            {
                throw new Exception("too small to be a valid subsequent chunk");
            }
            var instance = new SubsequentChunk();
            instance.Version = data[offset];
            if (instance.Version == 0)
            {
                throw new Exception("version not set");
            }
            instance.Flags = data[offset + 1];
            instance.Data = data;
            instance.DataOffset = offset + 2;
            instance.DataLength = length - 2;
            return instance;
        }
    }
}
