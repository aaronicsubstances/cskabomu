using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Wraps another reader to ensure a given amount of bytes are read.
    /// </summary>
    public class ContentLengthEnforcingCustomReader : ICustomReader
    {
        private readonly ICustomReader _wrappedReader;
        private readonly long _expectedLength;
        private long _bytesAlreadyRead;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="wrappedReader">the backing reader.</param>
        /// <param name="expectedLength">the expected number of bytes to guarantee or assert.
        /// Can be negative to indicate that the all remaining bytes in the backing reader
        /// should be returned.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        public ContentLengthEnforcingCustomReader(ICustomReader wrappedReader,
            long expectedLength)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _wrappedReader = wrappedReader;
            _expectedLength = expectedLength;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            int bytesToRead = length;
            if (_expectedLength >= 0)
            {
                // just in case content length is changed in between reads due
                // if content length becomes an editable property,
                // ensure bytesToRead will never become negative.
                bytesToRead = (int)Math.Max(0,
                    Math.Min(_expectedLength - _bytesAlreadyRead,
                    length));
            }

            // even if bytes to read is zero at this stage, still go ahead and call
            // wrapped body instead of trying to optimize by returning zero, so that
            // any error can be thrown.
            int bytesJustRead = await _wrappedReader.ReadBytes(data, offset, bytesToRead);

            // update record of number of bytes read.
            _bytesAlreadyRead += bytesJustRead;

            // if end of read is encountered, ensure that all
            // requested bytes have been read.
            var remainingBytesToRead = _expectedLength - _bytesAlreadyRead;
            if (bytesJustRead == 0 && remainingBytesToRead > 0)
            {
                var errorMsg = CustomIOException.CreateContentLengthNotSatisfiedErrorMessage(
                    _expectedLength) +
                    $" (could not read remaining {remainingBytesToRead} " +
                    "bytes before end of read)";

                throw new CustomIOException(errorMsg);
            }
            return bytesJustRead;
        }

        public Task CustomDispose()
        {
            return _wrappedReader.CustomDispose();
        }
    }
}
