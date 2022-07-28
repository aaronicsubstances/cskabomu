using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Wraps a quasi http body and forces its content type to a certain value, including null.
    /// All calls to ReadBytes() are forwarded to wrapped
    /// body without modification; only the content type value returned changes.
    /// </summary>
    public class ContentTypeOverrideBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;

        /// <summary>
        /// Creates new instance which imposes a content type on another quasi http body instance.
        /// </summary>
        /// <param name="wrappedBody">the quasi http bodyi instance whose content type is being overriden.</param>
        /// <param name="contentType">the overidding content type.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        public ContentTypeOverrideBody(IQuasiHttpBody wrappedBody, string contentType)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            ContentType = contentType;
        }

        /// <summary>
        /// Same as the content length of the quasi body instance whose content type is being overridden,
        /// ie the instance provided at construction time.
        /// </summary>
        public long ContentLength => _wrappedBody.ContentLength;

        /// <summary>
        /// Returns the overriding content type, ie the content type provided at construction time.
        /// </summary>
        public string ContentType { get; }

        public Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            return _wrappedBody.ReadBytes(data, offset, bytesToRead);
        }

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
