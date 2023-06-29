using Kabomu.Common;
using Kabomu.QuasiHttp.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Wraps another reader to ensure a given amount of bytes are read.
    /// Overriding content length can be -1 or any negative value, which
    /// indicates unknown length and will lead to reading all bytes of wrapped reader. 
    /// </summary>
    public class ContentLengthCheckCustomReader : ICustomReader
    {
        private readonly ICustomReader _wrappedReader;
        private readonly int _contentLength;
        private long _bytesAlreadyRead;

        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="wrappedReader">the backing reader.</param>
        /// <param name="contentLength">the expected number of bytes to guarantee.
        /// Can be negative to indicate unknown length.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        public ContentLengthCheckCustomReader(ICustomReader wrappedReader,
            int contentLength)
        {
            if (wrappedReader == null)
            {
                throw new ArgumentNullException(nameof(wrappedReader));
            }
            _wrappedReader = wrappedReader;
            _contentLength = contentLength;
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            int bytesToRead = length;
            if (_contentLength >= 0)
            {
                // just in case content length is changed in between reads due
                // if content length becomes an editable property,
                // ensure bytesToRead will never become negative.
                bytesToRead = (int)Math.Max(0,
                    Math.Min(_contentLength - _bytesAlreadyRead,
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
            var remainingBytesToRead = _contentLength - _bytesAlreadyRead;
            if (bytesJustRead == 0 && remainingBytesToRead > 0)
            {
                throw new ContentLengthNotSatisfiedException(
                    _contentLength,
                    $"could not read remaining {remainingBytesToRead} " +
                    $"bytes before end of read", null);
            }
            return bytesJustRead;
        }

        public Task CustomDispose()
        {
            return _wrappedReader.CustomDispose();
        }
    }
}
