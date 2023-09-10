using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Impl
{
    /// <summary>
    /// The standard decoder of quasi http bodies of unknown (ie negative)
    /// content lengths in the Kabomu library.
    /// Receives a source stream and assumes it consists of
    /// an unknown number of one or more body chunks, in which the last chunk has
    /// zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class BodyChunkDecodingStream : ReadableStreamBase
    {
        private readonly Stream _backingStream;
        private readonly byte[] _decodingBuffer;
        private int _chunkDataLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public BodyChunkDecodingStream(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            var minimumBodyChunkV1HeaderLength =
                // account for length of version 1 and separating comma.
                BodyChunkEncodingWriter.LengthOfEncodedBodyChunkLength +
                QuasiHttpProtocolUtils.ProtocolVersion01.Length +
                1;
            _decodingBuffer = new byte[minimumBodyChunkV1HeaderLength];
        }

        public override int ReadByte()
        {
            // once empty data chunk is seen, return -1 for all subsequent reads.
            if (_lastChunkSeen)
            {
                return -1;
            }

            if (_chunkDataLenRem == 0)
            {
                _chunkDataLenRem = FillDecodingBuffer();
                if (_chunkDataLenRem == 0)
                {
                    _lastChunkSeen = true;
                    return -1;
                }
            }

            int byteRead;
            try
            {
                byteRead = _backingStream.ReadByte();
                if (byteRead < 0)
                {
                    throw new CustomIOException("unexpected end of read");
                }
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "reading in chunk data", e);
            }
            _chunkDataLenRem--;
            return byteRead;
        }

        public override int Read(byte[] data, int offset, int length)
        {
            // once empty data chunk is seen, return 0 for all subsequent reads.
            if (_lastChunkSeen)
            {
                return 0;
            }

            if (_chunkDataLenRem == 0)
            {
                _chunkDataLenRem = FillDecodingBuffer();
                if (_chunkDataLenRem == 0)
                {
                    _lastChunkSeen = true;
                    return 0;
                }
            }

            int bytesToRead = Math.Min(_chunkDataLenRem, length);
            try
            {
                MiscUtils.ReadExactBytes(_backingStream, data, offset, bytesToRead);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "reading in chunk data", e);
            }
            _chunkDataLenRem -= bytesToRead;

            return bytesToRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            // once empty data chunk is seen, return 0 for all subsequent reads.
            if (_lastChunkSeen)
            {
                return 0;
            }

            if (_chunkDataLenRem == 0)
            {
                _chunkDataLenRem = await FillDecodingBufferAsync(cancellationToken);
                if (_chunkDataLenRem == 0)
                {
                    _lastChunkSeen = true;
                    return 0;
                }
            }

            int bytesToRead = Math.Min(_chunkDataLenRem, buffer.Length);
            try
            {
                await MiscUtils.ReadExactBytesAsync(_backingStream,
                    buffer.Slice(0, bytesToRead),
                    cancellationToken);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "reading in chunk data", e);
            }
            _chunkDataLenRem -= bytesToRead;

            return bytesToRead;
        }

        private int FillDecodingBuffer()
        {
            try
            {
                MiscUtils.ReadExactBytes(_backingStream,
                    _decodingBuffer, 0, _decodingBuffer.Length);
                return DecodeSubsequentChunkV1Header();
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "decoding a chunk header", e);
            }
        }

        private async Task<int> FillDecodingBufferAsync(
            CancellationToken cancellationToken)
        {
            try
            {
                await MiscUtils.ReadExactBytesAsync(_backingStream,
                    _decodingBuffer, 0, _decodingBuffer.Length,
                    cancellationToken);
                return DecodeSubsequentChunkV1Header();
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "decoding a chunk header", e);
            }
        }

        private int DecodeSubsequentChunkV1Header()
        {
            var csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                _decodingBuffer));
            if (csv.Count == 0)
            {
                throw new ArgumentException("invalid quasi http body chunk header");
            }
            var specialHeader = csv[0];
            if (specialHeader.Count < 2)
            {
                throw new ArgumentException("invalid quasi http body chunk header");
            }
            // validate version column as valid positive integer.
            int v;
            try
            {
                v = MiscUtils.ParseInt32(specialHeader[0]);
            }
            catch (FormatException)
            {
                throw new ArgumentException("invalid version field: " + specialHeader[0]);
            }
            if (v <= 0)
            {
                throw new ArgumentException("invalid nonnegative version number: " + v);
            }
            int lengthOfData;
            try
            {
                lengthOfData = MiscUtils.ParseInt32(specialHeader[1]);
            }
            catch (FormatException)
            {
                throw new ArgumentException("invalid quasi http body chunk length: " + specialHeader[1]);
            }
            if (lengthOfData < 0)
            {
                throw new ArgumentException("invalid negative quasi http body chunk length: " +
                    $"{lengthOfData}");
            }
            return lengthOfData;
        }
    }
}
