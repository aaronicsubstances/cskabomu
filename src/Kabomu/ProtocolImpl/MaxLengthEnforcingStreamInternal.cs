using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// Wraps another readable stream to ensure a given amount of bytes
    /// are not exceeded by reads.
    /// </summary>
    internal class MaxLengthEnforcingStreamInternal : ReadableStreamBaseInternal
    {
        private static readonly int DefaultMaxLength = 134_217_728;

        private readonly Stream _backingStream;
        private readonly int _maxLength;
        private int _bytesLeftToRead;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="backingStream"> the source stream</param>
        /// <param name="maxLength">the maximum number of bytes to read</param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="contentLength"/> argument is negative</exception>
        public MaxLengthEnforcingStreamInternal(Stream backingStream,
            int maxLength = 0)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            if (maxLength == 0)
            {
                maxLength = DefaultMaxLength;
            }
            else if (maxLength <= 0)
            {
                throw new ArgumentException(
                    $"max length cannot be negative: {maxLength}");
            }
            _backingStream = backingStream;
            _maxLength = maxLength;
            _bytesLeftToRead = maxLength + 1; // check for excess read.
        }

        public override int ReadByte()
        {
            int bytesToRead = Math.Min(_bytesLeftToRead, 1);

            int byteRead = -1;
            int bytesJustRead = 0;
            if (bytesToRead > 0)
            {
                byteRead = _backingStream.ReadByte();
                bytesJustRead = byteRead >= 0 ? 1 : 0;
            }
            UpdateState(bytesJustRead);
            return byteRead;
        }

        public override int Read(byte[] data, int offset, int length)
        {
            int bytesToRead = Math.Min(_bytesLeftToRead, length);

            // if bytes to read is zero at this stage and
            // the length requested is zero,
            // go ahead and call backing reader
            // (e.g. so that any error in backing reader can be thrown).
            int bytesJustRead = 0;
            if (bytesToRead > 0 || length == 0)
            {
                bytesJustRead = _backingStream.Read(
                    data, offset, bytesToRead);
            }
            UpdateState(bytesJustRead);
            return bytesJustRead;
        }

        public override async Task<int> ReadAsync(
            byte[] data, int offset, int length,
            CancellationToken cancellationToken = default)
        {
            int bytesToRead = Math.Min(_bytesLeftToRead, length);

            // if bytes to read is zero at this stage and
            // the length requested is zero,
            // go ahead and call backing reader
            // (e.g. so that any error in backing reader can be thrown).
            int bytesJustRead = 0;
            if (bytesToRead > 0 || length == 0)
            {
                bytesJustRead = await _backingStream.ReadAsync(
                    data, offset, bytesToRead, cancellationToken);
            }
            UpdateState(bytesJustRead);
            return bytesJustRead;
        }

        private void UpdateState(int bytesJustRead)
        {
            _bytesLeftToRead -= bytesJustRead;
            if (_bytesLeftToRead == 0)
            {
                throw new KabomuIOException(
                    $"stream size exceeds limit of {_maxLength} bytes");
            }
        }
    }
}