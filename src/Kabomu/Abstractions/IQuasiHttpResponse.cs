using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Represents the equivalent of an HTTP response entity: response status line,
    /// response headers, and response body.
    /// </summary>
    public interface IQuasiHttpResponse :
        IDisposable, IAsyncDisposable, ICustomDisposable
    {
        /// <summary>
        /// Gets or sets the equivalent of HTTP response status code.
        /// </summary>
        int StatusCode { get; set; }

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
        IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response status text or reason phrase.
        /// </summary>
        string HttpStatusMessage { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response version value.
        /// </summary>
        string HttpVersion { get; set; }

        /// <summary>
        /// Gets the number of bytes that the instance will supply,
        /// or -1 (actually any negative value) to indicate an unknown number of bytes.
        /// </summary>
        long ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        Stream Body { get; set; }

        /// <summary>
        /// Gets or sets any objects which may be of interest during response processing.
        /// </summary>
        IDictionary<string, object> Environment { get; set; }
    }
}
