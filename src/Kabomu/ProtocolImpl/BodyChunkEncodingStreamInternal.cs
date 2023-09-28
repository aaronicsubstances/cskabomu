using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// The standard encoder of http bodies of unknown (ie negative)
    /// content lengths in the Kabomu library.
    /// </summary>
    /// <remarks>
    /// Receives a source stream from which it generates
    /// an unknown number of one or more body chunks, 
    /// in which the last chunk has zero data length
    /// and all the previous ones have non-empty data.
    /// </remarks>
    internal class BodyChunkEncodingStreamInternal : ReadableStreamBaseInternal
    {
        private static readonly int DefaultMaxBodyChunkDataSize = 8_192;

        private readonly Stream _backingStream;
        private readonly byte[] _chunkData;
        private byte[] _chunkPrefix;
        private int _usedChunkDataLength;
        private int _usedOffset;
        private bool _endOfReadSeen;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public BodyChunkEncodingStreamInternal(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            _chunkData = new byte[DefaultMaxBodyChunkDataSize];
        }

        public override int ReadByte()
        {
            if (_endOfReadSeen)
            {
                return -1;
            }
            if (_chunkPrefix == null || 
                _usedOffset >= _chunkPrefix.Length + _usedChunkDataLength)
            {
                if (_chunkPrefix != null && _usedChunkDataLength == 0)
                {
                    _endOfReadSeen = true;
                    return -1;
                }
                FillFromSource();
            }
            return SupplyByteFromOutstanding();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_endOfReadSeen)
            {
                return 0;
            }
            if (_chunkPrefix == null ||
                _usedOffset >= _chunkPrefix.Length + _usedChunkDataLength)
            {
                if (_chunkPrefix != null && _usedChunkDataLength == 0)
                {
                    _endOfReadSeen = true;
                    return 0;
                }
                FillFromSource();
            }
            return FillFromOutstanding(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(
            byte[] data, int offset, int length,
            CancellationToken cancellationToken = default)
        {
            if (_endOfReadSeen)
            {
                return 0;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (_chunkPrefix == null ||
                _usedOffset >= _chunkPrefix.Length + _usedChunkDataLength)
            {
                if (_chunkPrefix != null && _usedChunkDataLength == 0)
                {
                    _endOfReadSeen = true;
                    return 0;
                }
                await FillFromSourceAsync(cancellationToken);
            }
            return FillFromOutstanding(data, offset, length);
        }

        private void FillFromSource()
        {
            _usedChunkDataLength = _backingStream.Read(_chunkData);
            _chunkPrefix = TlvUtils.EncodeTagLengthOnly(QuasiHttpCodec.TagForBody,
                _usedChunkDataLength);
            _usedOffset = 0;
        }

        private async Task FillFromSourceAsync(CancellationToken cancellationToken)
        {
            _usedChunkDataLength = await _backingStream.ReadAsync(_chunkData,
                cancellationToken);
            _chunkPrefix = TlvUtils.EncodeTagLengthOnly(QuasiHttpCodec.TagForBody,
                _usedChunkDataLength);
            _usedOffset = 0;
        }

        private int FillFromOutstanding(byte[] data, int offset, int length)
        {
            var nextChunkLength = Math.Min(length,
                _chunkPrefix.Length + _usedChunkDataLength - _usedOffset);
            for (int i = 0; i < nextChunkLength; i++)
            {
                if (_usedOffset < _chunkPrefix.Length)
                {
                    data[offset + i] = _chunkPrefix[_usedOffset];
                }
                else
                {
                    data[offset + i] = _chunkData[_usedOffset - _chunkPrefix.Length];
                }
                _usedOffset++;
            }
            return nextChunkLength;
        }

        private int SupplyByteFromOutstanding()
        {
            int nextByte;
            if (_usedOffset < _chunkPrefix.Length)
            {
                nextByte = _chunkPrefix[_usedOffset];
            }
            else
            {
                nextByte = _chunkData[_usedOffset - _chunkPrefix.Length];
            }
            _usedOffset++;
            return nextByte;
        }
    }
}
