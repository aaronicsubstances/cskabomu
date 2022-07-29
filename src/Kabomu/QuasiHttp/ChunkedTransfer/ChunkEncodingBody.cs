using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk encoder of byte streams of unknown lengths in the Kabomu library. Wraps a quasi http body
    /// to generate a byte stream which consists of an unknown number of one or more <see cref="SubsequentChunk"/> instances
    /// ordered consecutively, in which the last instances has zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkEncodingBody : IQuasiHttpBody
    {
        /// <summary>
        /// Constant used internally to indicate the number of bytes used to encode the length
        /// of a lead or subsequent chunk, which is 3.
        /// </summary>
        internal static readonly int LengthOfEncodedChunkLength = 3;

        /// <summary>
        /// Constant which communicates the largest chunk size possible with the standard chunk transfer 
        /// implementation in the Kabomu library, and that is currently the largest
        /// unsigned integer that can fit into 3 bytes.
        /// </summary>
        public static readonly int HardMaxChunkSizeLimit = 1 << (8 * LengthOfEncodedChunkLength) - 1;

        private readonly IQuasiHttpBody _wrappedBody;
        private readonly int _maxChunkSize;
        private bool _endOfReadSeen;

        /// <summary>
        /// Constructor for encoding data from another quasi http body instance into chunks.
        /// </summary>
        /// <param name="wrappedBody">the quasi http body to encode</param>
        /// <param name="maxChunkSize">the maximum size of each encoded chunk to be created</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="maxChunkSize"/> argument is zero or negative, or
        /// is larger than value of <see cref="HardMaxChunkSizeLimit"/> field.</exception>
        public ChunkEncodingBody(IQuasiHttpBody wrappedBody, int maxChunkSize)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            if (maxChunkSize > HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"max chunk size cannot exceed {HardMaxChunkSizeLimit}. received: {maxChunkSize}");
            }
            _wrappedBody = wrappedBody;
            _maxChunkSize = maxChunkSize;
        }

        /// <summary>
        /// Returns -1 to indicate unknown length.
        /// </summary>
        public long ContentLength => -1;

        /// <summary>
        /// Same as the content type of the body instance being encoded (ie the one provided at construction time).
        /// </summary>
        public string ContentType => _wrappedBody.ContentType;

        /// <summary>
        /// Helper function for writing quasi http headers to transports. Quasi http headers are encoded
        /// as the leading chunk before any subsequent chunk representing part of the data of an http body.
        /// </summary>
        /// <param name="transport">the quasi http transport to write to</param>
        /// <param name="connection">the particular connection of the transport to write to</param>
        /// <param name="chunk">the lead chunk containing http headers to be written</param>
        /// <param name="maxChunkSize">the maximum size of the lead chunk.</param>
        /// <returns>a task representing the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="transport"/> or
        /// the <paramref name="chunk"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The size of the data in <paramref name="chunk"/> argument 
        /// is larger than the <paramref name="maxChunkSize"/> argument, or is larger than value of
        /// <see cref="HardMaxChunkSizeLimit"/> field.</exception>
        public static async Task WriteLeadChunk(IQuasiHttpTransport transport, object connection,
             LeadChunk chunk, int maxChunkSize)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (chunk == null)
            {
                throw new ArgumentException("null chunk");
            }
            var slices = chunk.Serialize();
            int byteCount = 0;
            foreach (var slice in slices)
            {
                byteCount += slice.Length;
            }
            if (byteCount > maxChunkSize)
            {
                throw new ArgumentException($"headers larger than max chunk size of {maxChunkSize}");
            }
            if (byteCount > HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"headers larger than max chunk size limit of {HardMaxChunkSizeLimit}");
            }
            var encodedLength = new byte[LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await transport.WriteBytes(connection, encodedLength, 0, encodedLength.Length);
            await TransportUtils.WriteByteSlices(transport, connection, slices);
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            var chunkPrefix = new SubsequentChunk
            {
                Version = LeadChunk.Version01
            }.Serialize();
            var chunkPrefixLength = ByteUtils.CalculateSizeOfSlices(chunkPrefix);
            var reservedBytesToUse = LengthOfEncodedChunkLength + chunkPrefixLength;
            if (bytesToRead <= reservedBytesToUse)
            {
                if (bytesToRead < 0)
                {
                    throw new ArgumentException("invalid bytes to read");
                }
                else
                {
                    throw new ArgumentException($"require at least {reservedBytesToUse + 1} bytes to read");
                }
            }
            bytesToRead = Math.Min(bytesToRead, _maxChunkSize);
            if (bytesToRead <= reservedBytesToUse)
            {
                throw new ArgumentException("max chunk size too small to read and encode any number of bytes");
            }

            int bytesRead = await _wrappedBody.ReadBytes(data,
                offset + reservedBytesToUse, bytesToRead - reservedBytesToUse);

            if (bytesRead == 0)
            {
                if (_endOfReadSeen)
                {
                    return 0;
                }
                else
                {
                    _endOfReadSeen = true;
                }
            }
            ByteUtils.SerializeUpToInt64BigEndian(bytesRead + chunkPrefixLength, data, offset,
                LengthOfEncodedChunkLength);
            int sliceBytesWritten = 0;
            foreach (var slice in chunkPrefix)
            {
                Array.Copy(slice.Data, slice.Offset, 
                    data, offset + LengthOfEncodedChunkLength + sliceBytesWritten, 
                    slice.Length);
                sliceBytesWritten += slice.Length;
            }
            return bytesRead + reservedBytesToUse;
        }

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
