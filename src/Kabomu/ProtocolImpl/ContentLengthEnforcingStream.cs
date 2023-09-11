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
    /// Wraps another stream to ensure a given amount of bytes are read.
    /// </summary>
    public class ContentLengthEnforcingStream : ReadableStreamBase
    {
        private readonly Stream _backingStream;
        private readonly long _expectedLength;
        private long _bytesLeftToRead;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <param name="expectedLength">the expected number of bytes to guarantee or assert.
        /// Can be negative to indicate that the all remaining bytes in the backing reader
        /// should be returned.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        public ContentLengthEnforcingStream(Stream backingStream, long expectedLength)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
            _expectedLength = expectedLength;
            _bytesLeftToRead = expectedLength;
        }

        public override int ReadByte()
        {
            if (_bytesLeftToRead < 0)
            {
                return _backingStream.ReadByte();
            }

            int bytesToRead = Math.Min((int)_bytesLeftToRead, 1);

            int byteRead = -1;
            int bytesJustRead = 0;
            if (bytesToRead > 0)
            {
                byteRead = _backingStream.ReadByte();
                bytesJustRead = byteRead >= 0 ? 1 : 0;
            }
            UpdateState(bytesToRead, bytesJustRead);
            return byteRead;
        }

        public override int Read(byte[] data, int offset, int length)
        {
            if (_bytesLeftToRead < 0)
            {
                return _backingStream.Read(data, offset, length);
            }

            int bytesToRead = Math.Min((int)_bytesLeftToRead, length);

            // if bytes to read is zero at this stage,
            // go ahead and call backing reader
            // (e.g. so that any error in backing reader can be thrown),
            // unless the length requested is positive.
            int bytesJustRead = 0;
            if (bytesToRead > 0 || length == 0)
            {
                bytesJustRead = _backingStream.Read(
                    data, offset, bytesToRead);
            }
            UpdateState(bytesToRead, bytesJustRead);
            return bytesJustRead;
        }

        public override async Task<int> ReadAsync(
            byte[] data, int offset, int length,
            CancellationToken cancellationToken = default)
        {
            if (_bytesLeftToRead < 0)
            {
                return await _backingStream.ReadAsync(data, offset, length,
                    cancellationToken);
            }

            int bytesToRead = Math.Min((int)_bytesLeftToRead, length);

            // if bytes to read is zero at this stage,
            // go ahead and call backing reader
            // (e.g. so that any error in backing reader can be thrown),
            // unless the length requested is positive.
            int bytesJustRead = 0;
            if (bytesToRead > 0 || length == 0)
            {
                bytesJustRead = await _backingStream.ReadAsync(
                    data, offset, bytesToRead, cancellationToken);
            }
            UpdateState(bytesToRead, bytesJustRead);
            return bytesJustRead;
        }

        private void UpdateState(int bytesToRead, int bytesJustRead)
        {
            _bytesLeftToRead -= bytesJustRead;

            // if end of read is encountered, ensure that all
            // requested bytes have been read.
            bool endOfRead = bytesToRead > 0 && bytesJustRead == 0;
            if (endOfRead && _bytesLeftToRead > 0)
            {
                throw CustomIOException.CreateContentLengthNotSatisfiedError(
                    _expectedLength, _bytesLeftToRead);
            }
        }
    }
}
