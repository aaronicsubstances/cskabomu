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
        private readonly byte[] _chunkPrefix;
        private int _usedChunkPrefixLength;
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
            _chunkPrefix = BodyChunkEncodingWriter.AllocateBodyChunkHeaderBuffer();
        }

        public override int ReadByte()
        {
            if (_endOfReadSeen)
            {
                return -1;
            }
            if (_usedOffset >= _usedChunkPrefixLength + _usedChunkDataLength)
            {
                if (_usedChunkPrefixLength > 0 && _usedChunkDataLength == 0)
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
            if (_usedOffset >= _usedChunkPrefixLength + _usedChunkDataLength)
            {
                if (_usedChunkPrefixLength > 0 && _usedChunkDataLength == 0)
                {
                    _endOfReadSeen = true;
                    return 0;
                }
                FillFromSource();
            }
            return FillFromOutstanding(buffer);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            if (_endOfReadSeen)
            {
                return 0;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (_usedOffset >= _usedChunkPrefixLength + _usedChunkDataLength)
            {
                if (_usedChunkPrefixLength > 0 && _usedChunkDataLength == 0)
                {
                    _endOfReadSeen = true;
                    return 0;
                }
                await FillFromSourceAsync(cancellationToken);
            }
            return FillFromOutstanding(buffer);
        }

        private void FillFromSource()
        {
            _usedChunkDataLength = _backingStream.Read(_chunkData);
            _usedChunkPrefixLength = BodyChunkEncodingWriter.EncodeBodyChunkV1Header(
                _usedChunkDataLength, _chunkPrefix);
            _usedOffset = 0;
        }

        private async Task FillFromSourceAsync(CancellationToken cancellationToken)
        {
            _usedChunkDataLength = await _backingStream.ReadAsync(_chunkData,
                cancellationToken);
            _usedChunkPrefixLength = BodyChunkEncodingWriter.EncodeBodyChunkV1Header(
                _usedChunkDataLength, _chunkPrefix);
            _usedOffset = 0;
        }

        private int FillFromOutstanding(Memory<byte> buffer)
        {
            var nextChunkLength = Math.Min(buffer.Length,
                _usedChunkPrefixLength + _usedChunkDataLength - _usedOffset);
            var span = buffer.Span;
            for (int i = 0; i < nextChunkLength; i++)
            {
                if (_usedOffset < _usedChunkPrefixLength)
                {
                    span[i] = _chunkPrefix[_usedOffset];
                }
                else
                {
                    span[i] = _chunkData[_usedOffset - _usedChunkPrefixLength];
                }
                _usedOffset++;
            }
            return nextChunkLength;
        }

        private int SupplyByteFromOutstanding()
        {
            int nextByte;
            if (_usedOffset < _usedChunkPrefixLength)
            {
                nextByte = _chunkPrefix[_usedOffset];
            }
            else
            {
                nextByte = _chunkData[_usedOffset - _usedChunkPrefixLength];
            }
            _usedOffset++;
            return nextByte;
        }
    }
}
