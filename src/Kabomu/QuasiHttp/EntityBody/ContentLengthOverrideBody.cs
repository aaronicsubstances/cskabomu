using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Wraps a quasi http body and forces its content length to a certain value, including -1 (actually
    /// any negative value) to indicate unknown length. All calls to ReadBytes() are forwarded to wrapped
    /// body, and where the imposed content length is nonnegative, additional validation checks are
    /// performed to ensure that ReadBytes() call return 0 only when number of bytes equal to content length
    /// have been returned in total.
    /// </summary>
    public class ContentLengthOverrideBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;
        private long _bytesRemaining;

        /// <summary>
        /// Creates new instance which imposes a content length on another quasi http body instance.
        /// </summary>
        /// <param name="wrappedBody">the quasi http bodyi instance whose content length is being overriden.</param>
        /// <param name="contentLength">the overidding content length. can be negative, zero or positive.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        public ContentLengthOverrideBody(IQuasiHttpBody wrappedBody, long contentLength)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            ContentLength = contentLength;
            if (ContentLength >= 0)
            {
                _bytesRemaining = contentLength;
            }
            else
            {
                _bytesRemaining = -1;
            }
        }

        /// <summary>
        /// Gets the overriding content length which is equal to the value supplied at construction time.
        /// </summary>
        public long ContentLength { get; }

        /// <summary>
        /// Same as the content type of the body instance whose content length is being overridden, ie
        /// the instance provided at construction time.
        /// </summary>
        public string ContentType => _wrappedBody.ContentType;

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (_bytesRemaining >= 0)
            {
                bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
            }

            // even if bytes to read is zero at this stage, still go ahead and call
            // wrapped body instead of trying to optimize by returning zero, so that
            // any end of read error can be thrown.
            int bytesRead = await _wrappedBody.ReadBytes(data, offset, bytesToRead);

            if (_bytesRemaining > 0)
            {
                if (bytesRead == 0)
                {
                    throw new ContentLengthNotSatisfiedException(ContentLength,
                        $"could not read remaining {_bytesRemaining} " +
                        $"bytes before end of read", null);
                }
                _bytesRemaining -= bytesRead;
            }
            return bytesRead;
        }

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
