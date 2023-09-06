using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// The standard chunk decoder of byte streams in the Kabomu library.
    /// Receives a reader and assumes it consists of
    /// an unknown number of one or more chunks, in which the last chunk has
    /// zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkDecodingCustomReader : ICustomReader
    {
        private readonly QuasiHttpCodec Decoder = new QuasiHttpCodec();
        private readonly object _wrappedReader;
        private int _chunkDataLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Constructor for decoding chunks according to the custom
        /// chunk transfer protocol.
        /// </summary>
        /// <param name="wrappedReader">the source of bytes to decode which
        /// is acceptable by <see cref="QuasiHttpUtils.ReadBytes"/> function.</param>
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
                try
                {
                    _chunkDataLenRem = await Decoder.DecodeBodyChunkV1Header(
                        _wrappedReader);
                }
                catch (Exception e)
                {
                    throw new ChunkDecodingException("Failed to decode quasi http body while " +
                        "decoding a chunk header", e);
                }
                if (_chunkDataLenRem == 0)
                {
                    _lastChunkSeen = true;
                    return 0;
                }
            }

            bytesToRead = Math.Min(_chunkDataLenRem, bytesToRead);
            try
            {
                await QuasiHttpUtils.ReadBytesFully(_wrappedReader,
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
