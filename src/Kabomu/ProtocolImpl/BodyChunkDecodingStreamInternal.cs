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
    /// The standard decoder of quasi http bodies of unknown
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
        private readonly int _expectedTag;
        private int _chunkDataLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <param name="expectedTag"></param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public BodyChunkDecodingStreamInternal(Stream backingStream,
            int expectedTag)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            _expectedTag = expectedTag;
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
                _chunkDataLenRem = FetchNextTagAndLengthSync();
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
                _chunkDataLenRem = FetchNextTagAndLengthSync();
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
                _chunkDataLenRem = await FetchNextTagAndLength(cancellationToken);
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

        private int FetchNextTagAndLengthSync()
        {
            TlvUtils.ReadExpectedTagOnlySync(_backingStream, _expectedTag);
            return TlvUtils.ReadLengthOnlySync(_backingStream);
        }

        private async Task<int> FetchNextTagAndLength(
            CancellationToken cancellationToken)
        {
            await TlvUtils.ReadExpectedTagOnly(_backingStream,
                _expectedTag, cancellationToken);
            return await TlvUtils.ReadLengthOnly(_backingStream,
                cancellationToken);
        }
    }
}
