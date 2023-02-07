using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Wraps an instance of a quasi http body and forwards all method calls to it.
    /// </summary>
    internal class ProxyBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="wrappedBody">the quasi http instance to which all method calls will be forwarded</param>
        public ProxyBody(IQuasiHttpBody wrappedBody)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
        }

        /// <summary>
        /// Returns the content length of the instance provided at construction time.
        /// </summary>
        public long ContentLength => _wrappedBody.ContentLength;

        /// <summary>
        /// Returns the content type of the instance provided at construction time.
        /// </summary>
        public string ContentType => _wrappedBody.ContentType;

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
