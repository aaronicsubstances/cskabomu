using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides default implementation of <see cref="IQuasiHttpResponse"/> and
    /// <see cref="IQuasiHttpMutableResponse"/> interfaces.
    /// </summary>
    public class DefaultQuasiHttpResponse : IQuasiHttpMutableResponse
    {
        /// <summary>
        /// Gets or sets a value indicating response success: true for response success, false for response
        /// failure
        /// </summary>
        public bool StatusIndicatesSuccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a false response success value is due to
        /// a client error or server error: true for client error, false for server error.
        /// </summary>
        public bool StatusIndicatesClientError { get; set; }

        /// <summary>
        /// Gets or sets a value providing textual description of response success or failure. Equivalent
        /// to reason phrase of HTTP responses.
        /// </summary>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP response headers.
        /// </summary>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets the response body.
        /// </summary>
        public IQuasiHttpBody Body { get; set; }

        /// <summary>
        /// Gets or sets an HTTP response status code.
        /// </summary>
        public int HttpStatusCode { get; set; }

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
