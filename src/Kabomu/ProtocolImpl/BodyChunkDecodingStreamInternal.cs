using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// The standard decoder of quasi http bodies of unknown (ie negative)
    /// content lengths in the Kabomu library.
    /// </summary>
    /// <remarks>
    /// Receives a source stream and assumes it consists of
    /// an unknown number of one or more body chunks, in which the last chunk has
    /// zero data length and all the previous ones have non-empty data.
    /// </remarks>
    internal class BodyChunkDecodingStreamInternal : ReadableStreamBaseInternal
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
        public BodyChunkDecodingStreamInternal(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            var minimumBodyChunkV1HeaderLength =
                // account for length of version 1 and separating comma.
                BodyChunkEncodingWriter.LengthOfEncodedBodyChunkLength +
                QuasiHttpCodec.ProtocolVersion01.Length +
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

            int byteRead = _backingStream.ReadByte();
            if (byteRead < 0)
            {
                throw KabomuIOException.CreateEndOfReadError();
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
            bytesToRead = _backingStream.Read(data, offset, bytesToRead);
            if (bytesToRead <= 0)
            {
                throw KabomuIOException.CreateEndOfReadError();
            }
            _chunkDataLenRem -= bytesToRead;

            return bytesToRead;
        }

        public override async Task<int> ReadAsync(
            byte[] data, int offset, int length,
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

            int bytesToRead = Math.Min(_chunkDataLenRem, length);
            bytesToRead = await _backingStream.ReadAsync(data, offset, bytesToRead,
                cancellationToken);
            if (bytesToRead <= 0)
            {
                throw KabomuIOException.CreateEndOfReadError();
            }
            _chunkDataLenRem -= bytesToRead;

            return bytesToRead;
        }

        private int FillDecodingBuffer()
        {
            IOUtilsInternal.ReadBytesFullySync(_backingStream,
                _decodingBuffer, 0, _decodingBuffer.Length);
            return DecodeSubsequentChunkV1Header();
        }

        private async Task<int> FillDecodingBufferAsync(
            CancellationToken cancellationToken)
        {
            await IOUtilsInternal.ReadBytesFully(_backingStream,
                _decodingBuffer, 0, _decodingBuffer.Length,
                cancellationToken);
            return DecodeSubsequentChunkV1Header();
        }

        private int DecodeSubsequentChunkV1Header()
        {
            var csv = CsvUtils.Deserialize(MiscUtilsInternal.BytesToString(
                _decodingBuffer));
            if (csv.Count == 0 || csv[0].Count < 2 ||
                csv[0][0] != QuasiHttpCodec.ProtocolVersion01)
            {
                throw new KabomuIOException("invalid quasi http body chunk header");
            }
            var lengthOfDataStr = csv[0][1];
            int lengthOfData;
            try
            {
                lengthOfData = MiscUtilsInternal.ParseInt32(lengthOfDataStr);
            }
            catch (FormatException)
            {
                throw new KabomuIOException("invalid quasi http body chunk length: " +
                    lengthOfDataStr);
            }
            if (lengthOfData < 0)
            {
                throw new KabomuIOException("invalid quasi http body chunk length: " +
                    $"{lengthOfData}");
            }
            return lengthOfData;
        }
    }
}
