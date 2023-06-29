using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// The standard chunk encoder of byte streams of unknown lengths in the Kabomu library. Wraps a quasi http body
    /// to generate a byte stream which consists of an unknown number of one or more <see cref="SubsequentChunk"/> instances
    /// ordered consecutively, in which the last instances has zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkEncodingCustomReader : ICustomReader
    {
        private readonly ICustomReader _wrappedReader;
        private readonly int _maxChunkSize;
        private bool _endOfReadSeen;

        /// <summary>
        /// Constructor for encoding data from another reader instance into chunks.
        /// </summary>
        /// <param name="wrappedReader">the reader to encode</param>
        /// <param name="maxChunkSize">the maximum size of each encoded chunk to be created</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="maxChunkSize"/> argument is zero or negative, or
        /// is larger than value of <see cref="HardMaxChunkSizeLimit"/> field.</exception>
        public ChunkEncodingCustomReader(ICustomReader wrappedReader, int maxChunkSize)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            if (maxChunkSize > ChunkedTransferUtils.HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"max chunk size cannot exceed {ChunkedTransferUtils.HardMaxChunkSizeLimit}. received: {maxChunkSize}");
            }
            _wrappedReader = wrappedReader;
            _maxChunkSize = maxChunkSize;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_endOfReadSeen)
            {
                return 0;
            }
            int bytesRead = await ChunkedTransferUtils.ReadNextSubsequentChunk(_wrappedReader,
                _maxChunkSize, data, offset, length);
            if (bytesRead == ChunkedTransferUtils.ReservedBytesToUse)
            {
                _endOfReadSeen = true;
            }
            return bytesRead;
        }

        public Task CustomDispose()
        {
            return _wrappedReader.CustomDispose();
        }
    }
}
