using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Wraps a quasi http body and optionally forces its content type or
    /// content length to fixed values.
    /// </summary>
    /// <remarks>
    /// if content length is fixed to negative, then all calls to ReadBytes() are forwarded to wrapped
    /// body without modification; else any nonnegative content length will be enforced.
    /// </remarks>
    public class OverridingProxyBody : IQuasiHttpBody, IBytesAlreadyReadProviderInternal
    {
        private readonly IQuasiHttpBody _wrappedBody;
        private long _contentLength;
        private string _contentType;

        /// <summary>
        /// Creates new instance which imposes a content type on another quasi http body instance.
        /// </summary>
        /// <param name="wrappedBody">the backing quasi http instance</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        public OverridingProxyBody(IQuasiHttpBody wrappedBody)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            _wrappedBody = wrappedBody;
            _contentLength = _wrappedBody.ContentLength;
            _contentType = _wrappedBody.ContentType;
        }

        /// <summary>
        /// Retrieves corresponding value from the backing quasi body, 
        /// unless <see cref="IsContentLengthProxied"/> property is set to 
        /// false, in which case any value stored in setter will be
        /// returned.
        /// </summary>
        /// <remarks>
        /// Note that setter always stores value in argument.
        /// </remarks>
        public long ContentLength
        {
            get
            {
                if (IsContentLengthProxied)
                {
                    return _wrappedBody.ContentLength;
                }
                else
                {
                    return _contentLength;
                }
            }
            set
            {
                _contentLength = value;
            }
        }

        /// <summary>
        /// Retrieves corresponding value from the backing quasi body, 
        /// unless <see cref="IsContentTypeProxied"/> property is set to 
        /// false, in which case any value stored in setter will be
        /// returned.
        /// </summary>
        /// <remarks>
        /// Note that setter always stores value in argument.
        /// </remarks>
        public string ContentType
        {
            get
            {
                if (IsContentTypeProxied)
                {
                    return _wrappedBody.ContentType;
                }
                else
                {
                    return _contentType;
                }
            }
            set
            {
                _contentType = value;
            }
        }

        public bool IsContentLengthProxied { get; set; }

        public bool IsContentTypeProxied { get; set; }

        long IBytesAlreadyReadProviderInternal.BytesAlreadyRead { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            return EntityBodyUtilsInternal.PerformGeneralRead(this,
                length, bytesToRead => _wrappedBody.ReadBytes(
                    data, offset, bytesToRead));
        }

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
