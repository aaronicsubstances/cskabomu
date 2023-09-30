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
    internal class ContentLengthEnforcingStreamInternal : ReadableStreamBaseInternal
    {
        private readonly Stream _backingStream;
        private readonly long _contentLength;
        private long _bytesLeftToRead;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="backingStream">the source stream</param>
        /// <param name="contentLength">the expected number of bytes to guarantee or assert.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="backingStream"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="contentLength"/> argument is negative</exception>
        public ContentLengthEnforcingStreamInternal(Stream backingStream, long contentLength)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            if (contentLength < 0)
            {
                throw new ArgumentException(
                    $"content length cannot be negative: {contentLength}");
            }
            _backingStream = backingStream;
            _contentLength = contentLength;
            _bytesLeftToRead = contentLength;
        }

        public override int ReadByte()
        {
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
            int bytesToRead = Math.Min((int)_bytesLeftToRead, length);

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
            UpdateState(bytesToRead, bytesJustRead);
            return bytesJustRead;
        }

        public override async Task<int> ReadAsync(
            byte[] data, int offset, int length,
            CancellationToken cancellationToken = default)
        {
            int bytesToRead = Math.Min((int)_bytesLeftToRead, length);

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
                throw new KabomuIOException($"insufficient bytes available to satisfy " +
                    $"content length of {_contentLength} bytes (could not read remaining " +
                    $"{_bytesLeftToRead} bytes before end of read)");
            }
        }
    }
}
