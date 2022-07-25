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

        public ContentTypeOverrideBody(IQuasiHttpBody wrappedBody, string contentType)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            ContentType = contentType;
        }

        public long ContentLength => _wrappedBody.ContentLength;
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
