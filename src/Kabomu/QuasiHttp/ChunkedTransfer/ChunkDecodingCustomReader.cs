using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk decoder of byte streams in the Kabomu library.
    /// Receives a reader and assumes it consists of
    /// an unknown number of one or more chunks, in which the last chunk has
    /// zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkDecodingCustomReader : ICustomReader
    {
        private readonly object _wrappedReader;
        private readonly CustomChunkedTransferCodec _chunkTransferUtils = new CustomChunkedTransferCodec();
        private int _chunkDataLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Constructor for decoding chunks previously encoded in the data of another instance.
        /// </summary>
        /// <param name="wrappedReader">the source of bytes to decode which
        /// is acceptable by <see cref="IOUtils.ReadBytes"/> function.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        public ChunkDecodingCustomReader(object wrappedReader)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _wrappedReader = wrappedReader;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            // once empty data chunk is seen, return 0 for all subsequent reads.
            if (_lastChunkSeen)
            {
                return 0;
            }

            if (_chunkDataLenRem == 0)
            {
                _chunkDataLenRem = await _chunkTransferUtils.DecodeSubsequentChunkV1Header(
                    null, _wrappedReader);
                if (_chunkDataLenRem == 0)
                {
                    _lastChunkSeen = true;
                    return 0;
                }
            }

            bytesToRead = Math.Min(_chunkDataLenRem, bytesToRead);
            try
            {
                await IOUtils.ReadBytesFully(_wrappedReader,
                    data, offset, bytesToRead);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "reading in chunk data", e);
            }
            _chunkDataLenRem -= bytesToRead;

            return bytesToRead;
        }
    }
}
