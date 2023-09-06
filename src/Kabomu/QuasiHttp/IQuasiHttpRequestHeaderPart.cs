using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpRequestHeaderPart
    {
        /// <summary>
        /// Gets the equivalent of request target component of HTTP request line.
        /// </summary>
        string Target { get; }

        /// <summary>
        /// Gets the equivalent of HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP/1.1, headers are case-sensitive and lower-cased
        /// header names are recommended
        /// <para></para>
        /// Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the request body.
        /// </remarks>
        IDictionary<string, IList<string>> Headers { get; }

        /// <summary>
        /// Gets an HTTP method value.
        /// </summary>
        string HttpMethod { get; }

        /// <summary>
        /// Gets an HTTP request version value.
        /// </summary>
        string HttpVersion { get; }

        long ContentLength { get; }
    }
}
