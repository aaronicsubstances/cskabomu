using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpRequestHeaderPart : IQuasiHttpRequestHeaderPart
    {
        /// <summary>
        /// Gets or sets the equivalent of request target component of HTTP request line.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP/1.1, headers are case-sensitive and lower-cased
        /// header names are recommended
        /// <para></para>
        /// Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the request body.
        /// </remarks>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets an HTTP method value.
        /// </summary>
        public string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets an HTTP request version value.
        /// </summary>
        public string HttpVersion { get; set; }

        public long ContentLength { get; set; }
    }
}
