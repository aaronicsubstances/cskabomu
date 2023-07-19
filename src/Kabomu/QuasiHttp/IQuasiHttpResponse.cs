using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Represents the equivalent of an HTTP response entity: response status line,
    /// response headers, and response body.
    /// </summary>
    public interface IQuasiHttpResponse : ICustomDisposable
    {
        /// <summary>
        /// Gets the equivalent of HTTP response status code.
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// Gets the equivalent of HTTP response headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, headers are case-sensitive. Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the response body.
        /// </remarks>
        IDictionary<string, IList<string>> Headers { get; }

        /// <summary>
        /// Gets the response body.
        /// </summary>
        IQuasiHttpBody Body { get; }

        /// <summary>
        /// Gets an HTTP response status text or reason phrase.
        /// </summary>
        string HttpStatusMessage { get; }

        /// <summary>
        /// Gets an HTTP response version value.
        /// </summary>
        string HttpVersion { get; }

        /// Gets any objects which may be of interest during response processing.
        IDictionary<string, object> Environment { get; }
    }
}
