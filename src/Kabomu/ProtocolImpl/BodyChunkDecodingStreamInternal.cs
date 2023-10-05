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
        private readonly int _tagToIgnore;
        private int _chunkDataLenRem;
        private bool _lastChunkSeen;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <param name="expectedTag"></param>
        /// <param name="tagToIgnore"></param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public BodyChunkDecodingStreamInternal(Stream backingStream,
            int expectedTag, int tagToIgnore)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            _expectedTag = expectedTag;
            _tagToIgnore = tagToIgnore;
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
            var tag = ReadTagOnlySync();
            if (tag == _tagToIgnore)
            {
                ReadAwayTagValueSync();
                tag = ReadTagOnlySync();
            }
            if (tag != _expectedTag)
            {
                throw new KabomuIOException("unexpected tag: expected " +
                    $"{_expectedTag} but found {tag}");
            }
            return ReadLengthOnlySync();
        }

        private async Task<int> FetchNextTagAndLength(
            CancellationToken cancellationToken)
        {
            var tag = await ReadTagOnly(cancellationToken);
            if (tag == _tagToIgnore)
            {
                await ReadAwayTagValue(cancellationToken);
                tag = await ReadTagOnly(cancellationToken);
            }
            if (tag != _expectedTag)
            {
                throw new KabomuIOException("unexpected tag: expected " +
                    $"{_expectedTag} but found {tag}");
            }
            return await ReadLengthOnly(cancellationToken);
        }

        private async Task ReadAwayTagValue(CancellationToken cancellationToken)
        {
            int length = await ReadLengthOnly(
                cancellationToken);
            if (length > 0)
            {
                await TlvUtils.CreateContentLengthEnforcingStream(
                        _backingStream, length)
                    .CopyToAsync(Stream.Null, cancellationToken);
            }
        }

        private void ReadAwayTagValueSync()
        {
            int length = ReadLengthOnlySync();
            if (length > 0)
            {
                TlvUtils.CreateContentLengthEnforcingStream(
                        _backingStream, length)
                    .CopyTo(Stream.Null);
            }
        }

        private async Task<int> ReadTagOnly(
            CancellationToken cancellationToken)
        {
            var encodedTag = new byte[4];
            await IOUtilsInternal.ReadBytesFully(_backingStream,
                encodedTag, 0, encodedTag.Length,
                cancellationToken);
            return DecodeTagObtainedFromStream(encodedTag, 0);
        }

        private int ReadTagOnlySync()
        {
            var encodedTag = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(_backingStream,
                encodedTag, 0, encodedTag.Length);
            return DecodeTagObtainedFromStream(encodedTag, 0);
        }

        private async Task<int> ReadLengthOnly(
            CancellationToken cancellationToken)
        {
            var encodedLen = new byte[4];
            await IOUtilsInternal.ReadBytesFully(_backingStream,
                encodedLen, 0, encodedLen.Length,
                cancellationToken);
            return DecodeLengthObtainedFromStream(encodedLen, 0);
        }

        private int ReadLengthOnlySync()
        {
            var encodedLen = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(_backingStream,
                encodedLen, 0, encodedLen.Length);
            return DecodeLengthObtainedFromStream(encodedLen, 0);
        }

        private static int DecodeTagObtainedFromStream(byte[] data, int offset)
        {
            int tag = MiscUtilsInternal.DeserializeInt32BE(
                data, offset);
            if (tag <= 0)
            {
                throw new KabomuIOException("invalid tag: " +
                    tag);
            }
            return tag;
        }

        private static int DecodeLengthObtainedFromStream(byte[] data, int offset)
        {
            int decodedLength = MiscUtilsInternal.DeserializeInt32BE(
                data, offset);
            if (decodedLength < 0)
            {
                throw new KabomuIOException("invalid tag value length: " +
                    decodedLength);
            }
            return decodedLength;
        }
    }
}
