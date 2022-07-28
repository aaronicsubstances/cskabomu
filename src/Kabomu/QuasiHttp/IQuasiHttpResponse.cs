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
    public interface IQuasiHttpResponse
    {
        /// <summary>
        /// Gets a value indicating response success: true for response success, false for response
        /// failure
        /// </summary>
        /// <remarks>
        /// Equivalent to HTTP status code 200-299.
        /// </remarks>
        bool StatusIndicatesSuccess { get; }

        /// <summary>
        /// Gets a value indicating whether a false response success value is due to
        /// a client error or server error: true for client error, false for server error.
        /// </summary>
        /// <remarks>
        /// Equivalent to HTTP status code 400-499 if true, and 500 and above if false.
        /// </remarks>
        bool StatusIndicatesClientError { get; }

        /// <summary>
        /// Gets a value providing textual description of response success or failure. Equivalent
        /// to reason phrase of HTTP responses.
        /// </summary>
        string StatusMessage { get; }

        /// <summary>
        /// Gets the equivalent of HTTP response headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the response body.
        /// </remarks>
        IDictionary<string, List<string>> Headers { get; }

        /// <summary>
        /// Gets the response body.
        /// </summary>
        IQuasiHttpBody Body { get; }

        /// <summary>
        /// Gets an HTTP response status code.
        /// </summary>
        int HttpStatusCode { get; }

        /// <summary>
        /// Gets an HTTP response version value.
        /// </summary>
        string HttpVersion { get; }

        /// <summary>
        /// Ends reading on the Body property, and releases any resources held by the quasi http response
        /// implementation.
        /// </summary>
        /// <remarks>
        /// Must be called when response streaming is enabled in <see cref="Client.IQuasiHttpClient"/> to prevent memory leaks.
        /// </remarks>
        /// <returns>a task representing the asynchronous close operation</returns>
        Task Close();
    }
}
