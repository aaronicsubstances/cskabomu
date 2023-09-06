using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpResponseHeaderPart
    {
        /// <summary>
        /// Gets or sets the equivalent of HTTP response status code.
        /// </summary>
        int StatusCode { get; }

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
        IDictionary<string, IList<string>> Headers { get; }

        /// <summary>
        /// Gets or sets an HTTP response status text or reason phrase.
        /// </summary>
        string HttpStatusMessage { get; }

        /// <summary>
        /// Gets or sets an HTTP response version value.
        /// </summary>
        string HttpVersion { get; }

        long ContentLength { get; }
    }
}
