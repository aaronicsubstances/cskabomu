using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Represents the equivalent of an HTTP request entity: request line,
    /// request headers, and request body.
    /// </summary>
    public interface IQuasiHttpRequest
    {
        /// <summary>
        /// Gets the equivalent of path component of HTTP request line.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets the equivalent of HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the request body.
        /// </remarks>
        IDictionary<string, IList<string>> Headers { get; }

        /// <summary>
        /// Gets the request body.
        /// </summary>
        IQuasiHttpBody Body { get; }

        /// <summary>
        /// Gets an HTTP method value.
        /// </summary>
        string HttpMethod { get; }

        /// <summary>
        /// Gets an HTTP request version value.
        /// </summary>
        string HttpVersion { get; }
    }
}
