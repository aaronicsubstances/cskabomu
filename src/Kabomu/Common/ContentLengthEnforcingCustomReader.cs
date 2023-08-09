using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Wraps another reader to ensure a given amount of bytes are read.
    /// </summary>
    public class ContentLengthEnforcingCustomReader : ICustomReader
    {
        private readonly object _wrappedReader;
        private readonly long _expectedLength;
        private readonly bool _answerZeroByteReadsFromBackingReader;
        private long _bytesAlreadyRead;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="wrappedReader">the backing reader.</param>
        /// <param name="expectedLength">the expected number of bytes to guarantee or assert.
        /// Can be negative to indicate that the all remaining bytes in the backing reader
        /// should be returned.</param>
        /// <param name="answerZeroByteReadsFromBackingReader">pass true
        /// if a request to read zero bytes should be passed onto backing reader;
        /// or pass false to immediately return zero, which is the default.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedReader"/> argument is null.</exception>
        public ContentLengthEnforcingCustomReader(object wrappedReader,
            long expectedLength, bool answerZeroByteReadsFromBackingReader = false)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _wrappedReader = wrappedReader;
            _expectedLength = expectedLength;
            _answerZeroByteReadsFromBackingReader = answerZeroByteReadsFromBackingReader;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            int bytesToRead = length;
            if (_expectedLength >= 0)
            {
                // just in case content length is changed in between reads
                // if content length becomes an editable property,
                // ensure bytesToRead will never become negative.
                bytesToRead = (int)Math.Max(0,
                    Math.Min(_expectedLength - _bytesAlreadyRead,
                    length));
            }

            // if bytes to read is zero at this stage, decide on whether or
            // not to go ahead and call backing reader
            // so that any error in backing reader can be thrown.
            bool proceedWithUnderlyingRead;
            if (bytesToRead > 0)
            {
                proceedWithUnderlyingRead = true;
            }
            else if (length > 0)
            {
                proceedWithUnderlyingRead = false;
            }
            else
            {
                proceedWithUnderlyingRead = _answerZeroByteReadsFromBackingReader;
            }
            int bytesJustRead = 0;
            if (proceedWithUnderlyingRead)
            {
                bytesJustRead = await IOUtils.ReadBytes(_wrappedReader,
                    data, offset, bytesToRead);
            }

            // update record of number of bytes read.
            _bytesAlreadyRead += bytesJustRead;

            // if end of read is encountered, ensure that all
            // requested bytes have been read.
            var remainingBytesToRead = _expectedLength - _bytesAlreadyRead;
            if (bytesToRead > 0 && bytesJustRead == 0 && remainingBytesToRead > 0)
            {
                throw CustomIOException.CreateContentLengthNotSatisfiedError(
                    _expectedLength);
            }
            return bytesJustRead;
        }
    }
}
