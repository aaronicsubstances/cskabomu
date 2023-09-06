using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Kabomu.Abstractions;
using Kabomu.Exceptions;

namespace Kabomu
{
    /// <summary>
    /// Wraps another reader to ensure a given amount of bytes are read.
    /// </summary>
    public class ContentLengthEnforcingCustomReader : ICustomReader
    {
        private readonly object _wrappedReader;
        private readonly long _expectedLength;
        private long _bytesLeftToRead;
        private CustomIOException _endOfReadError;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="wrappedReader">the backing reader.</param>
        /// <param name="expectedLength">the expected number of bytes to guarantee or assert.
        /// Can be negative to indicate that the all remaining bytes in the backing reader
        /// should be returned.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        public ContentLengthEnforcingCustomReader(object wrappedReader, long expectedLength)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _wrappedReader = wrappedReader;
            _expectedLength = expectedLength;
            _bytesLeftToRead = expectedLength;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (_endOfReadError != null)
            {
                throw _endOfReadError;
            }

            if (_bytesLeftToRead < 0)
            {
                return await QuasiHttpUtils.ReadBytes(_wrappedReader, data, offset, length);
            }

            int bytesToRead = Math.Min((int)_bytesLeftToRead, length);

            // if bytes to read is zero at this stage,
            // go ahead and call backing reader
            // (e.g. so that any error in backing reader can be thrown),
            // unless the length requested is positive.
            int bytesJustRead = 0;
            if (bytesToRead > 0 || length == 0)
            {
                bytesJustRead = await QuasiHttpUtils.ReadBytes(_wrappedReader,
                    data, offset, bytesToRead);
            }

            _bytesLeftToRead -= bytesJustRead;

            // if end of read is encountered, ensure that all
            // requested bytes have been read.
            bool endOfRead = bytesToRead > 0 && bytesJustRead == 0;
            if (endOfRead && _bytesLeftToRead > 0)
            {
                _endOfReadError = CustomIOException.CreateContentLengthNotSatisfiedError(
                    _expectedLength, _bytesLeftToRead);
                throw _endOfReadError;
            }
            return bytesJustRead;
        }
    }
}
