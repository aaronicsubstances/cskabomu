using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk decoder of byte streams in the Kabomu library. Receives a reader and assumes it consists of
    /// an unknown number of one or more chunks, in which the last chunk has zero data length
    /// and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkDecodingCustomReader : ICustomReader
    {
        private readonly ICustomReader _wrappedReader;
        private readonly int _maxChunkSize;
        private readonly byte[] _chunkHeaderBuffer;
        private int _chunkDataLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Constructor for decoding chunks previously encoded in the data of another instance.
        /// </summary>
        /// <param name="wrappedReader">the source of bytes to decode.</param>
        /// <param name="maxChunkSize">the maximum allowable size of a chunk seen in the body instance being decoded.
        /// NB: values less than 64KB are always accepted, and so this parameter imposes a maximum only on chunks
        /// with lengths greater than 64KB.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        public ChunkDecodingCustomReader(ICustomReader wrappedReader, int maxChunkSize)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            if (maxChunkSize < ChunkedTransferUtils.DefaultMaxChunkSizeLimit)
            {
                maxChunkSize = ChunkedTransferUtils.DefaultMaxChunkSizeLimit;
            }
            _wrappedReader = wrappedReader;
            _maxChunkSize = maxChunkSize;
            _chunkHeaderBuffer = new byte[10];
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
                _chunkDataLenRem = await ChunkedTransferUtils.DecodeSubsequentChunkHeader(
                    _wrappedReader, _chunkHeaderBuffer, _maxChunkSize);
                if (_chunkDataLenRem == 0)
                {
                    _lastChunkSeen = true;
                    return 0;
                }
            }

            bytesToRead = Math.Min(_chunkDataLenRem, bytesToRead);
            await IOUtils.ReadBytesFully(_wrappedReader,
                data, offset, bytesToRead);
            _chunkDataLenRem -= bytesToRead;

            return bytesToRead;
        }

        public Task CustomDispose()
        {
            return _wrappedReader.CustomDispose();
        }
    }
}
