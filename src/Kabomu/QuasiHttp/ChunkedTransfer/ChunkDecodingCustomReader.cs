using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk decoder of byte streams in the Kabomu library. Wraps a quasi http body and assumes it consists of
    /// an unknown number of one or more <see cref="SubsequentChunk"/> instances ordered consecutively, in which the last
    /// instances has zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkDecodingCustomReader : ICustomReader
    {
        private readonly ICustomReader _wrappedReader;
        private readonly int _maxChunkSize;
        private readonly byte[] _encodedLengthReceiver;
        private int _chunkLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Constructor for decoding chunks previously encoded in the data of another instance.
        /// </summary>
        /// <param name="wrappedReader">the source of bytes to decode.</param>
        /// <param name="maxChunkSize">the maximum allowable size of a chunk seen in the body instance being decoded.
        /// NB: values less than 64KB are always accepted, and so this parameter imposes a maximum only on chunks
        /// with lengths greater than 64KB.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="maxChunkSize"/> argument is zero or negative.</exception>
        public ChunkDecodingCustomReader(ICustomReader wrappedReader, int maxChunkSize)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            _wrappedReader = wrappedReader;
            _maxChunkSize = maxChunkSize;
            _encodedLengthReceiver = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            // once empty data chunk is seen, return 0 for all subsequent reads.
            if (_lastChunkSeen)
            {
                return 0;
            }

            if (_chunkLenRem == 0)
            {
                try
                {
                    await IOUtils.ReadBytesFully(_wrappedReader,
                       _encodedLengthReceiver, 0, _encodedLengthReceiver.Length);
                }
                catch (Exception e)
                {
                    throw new ChunkDecodingException("Failed while " +
                        "reading a chunk length specification", e);
                }

                var chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(
                    _encodedLengthReceiver, 0, _encodedLengthReceiver.Length, true);
                ChunkedTransferUtils.ValidateChunkLength(chunkLen, _maxChunkSize, "Failed to decode quasi http body");

                if (chunkLen == 0)
                {
                    _lastChunkSeen = true;
                    return 0;
                }

                _chunkLenRem = chunkLen;
            }

            bytesToRead = Math.Min(_chunkLenRem, bytesToRead);
            try
            {
                await IOUtils.ReadBytesFully(_wrappedReader,
                    data, offset, bytesToRead);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed while " +
                    "reading in chunk data", e);
            }
            _chunkLenRem -= bytesToRead;

            return bytesToRead;
        }

        public Task CustomDispose()
        {
            return _wrappedReader.CustomDispose();
        }
    }
}
