using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents the equivalent of an HTTP request entity: request line,
    /// request headers, and request body.
    /// </summary>
    public interface IQuasiHttpRequest :
        IDisposable, IAsyncDisposable,ICustomDisposable
    {
        /// <summary>
        /// Gets or sets the equivalent of request target component of HTTP request line.
        /// </summary>
        string Target { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP/1.0, headers are case-sensitive and lower-cased
        /// header names are recommended
        /// <para></para>
        /// Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the request body.
        /// </remarks>
        IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets an HTTP method value.
        /// </summary>
        string HttpMethod { get; set; }

        /// <summary>
        /// Gets or sets an HTTP request version value.
        /// </summary>
        string HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes that the instance will supply,
        /// or -1 (actually any negative value) to indicate an unknown number of
        /// response bytes.
        /// </summary>
        /// <remarks>
        /// For requests, negative values are equivalent to zero content length.
        /// </remarks>
        long ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the request body.
        /// </summary>
        Stream Body { get; set; }

        /// <summary>
        /// Gets or sets any objects which may be of interest during request processing.
        /// </summary>
        IDictionary<string, object> Environment { get; set; }
    }
}
