using Kabomu.Common;
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
    public interface IQuasiHttpRequest : ICustomDisposable
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
        /// Gets the request body.
        /// </summary>
        IQuasiHttpBody Body { get; }

        /// <summary>
        /// Gets an HTTP method value.
        /// </summary>
        string Method { get; }

        /// <summary>
        /// Gets an HTTP request version value.
        /// </summary>
        string HttpVersion { get; }

        /// Gets any objects which may be of interest during request processing.
        IDictionary<string, object> Environment { get; }
    }
}
