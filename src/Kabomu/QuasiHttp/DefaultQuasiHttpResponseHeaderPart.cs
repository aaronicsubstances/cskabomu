using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpResponseHeaderPart : IQuasiHttpResponseHeaderPart
    {
        /// <summary>
        /// Gets or sets the equivalent of HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP response headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP/1.1, headers are case-sensitive and lower-cased
        /// header names are recommended
        /// <para></para>
        /// Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the response body.
        /// </remarks>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response status text or reason phrase.
        /// </summary>
        public string HttpStatusMessage { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response version value.
        /// </summary>
        public string HttpVersion { get; set; }

        public long ContentLength { get; set; }
    }
}
