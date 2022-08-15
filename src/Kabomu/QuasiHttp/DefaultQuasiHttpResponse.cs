using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpResponse"/> interface
    /// in which properties of the interface are mutable.
    /// </summary>
    public class DefaultQuasiHttpResponse : IQuasiHttpResponse
    {
        /// <summary>
        /// Status code value of 200, equivalent to HTTP status code 200 OK.
        /// </summary>
        public static readonly int StatusCodeOk = 200;

        /// <summary>
        /// Status code value of 400, equivalent to HTTP status code 400 Bad Request.
        /// </summary>
        public static readonly int StatusCodeClientError = 400;

        /// <summary>
        /// Status code value of 500, equivalent to HTTP status code 500 Internal Server Error.
        /// </summary>
        public static readonly int StatusCodeServerError = 500;

        /// <summary>
        /// Gets or sets the equivalent of HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP response headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, headers are case-sensitive. Also, setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the response body.
        /// </remarks>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        public IQuasiHttpBody Body { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response status text or reason phrase.
        /// </summary>
        public string HttpStatusMessage { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response version value.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets a native cancellation handle that will be cancelled when
        /// Close() is called.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Cancels the CancellationTokenSource property and ends reading on the Body property.
        /// </summary>
        /// <returns>a task representing the asynchronous close operation</returns>
        public Task Close()
        {
            CancellationTokenSource?.Cancel();
            var endReadTask = Body?.EndRead();
            return endReadTask ?? Task.CompletedTask;
        }
    }
}
