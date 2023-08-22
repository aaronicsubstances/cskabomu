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
        /// Gets or sets any objects which may be of interest to the code which
        /// will process this response instance.
        /// </summary>
        public IDictionary<string, object> Environment { get; set; }

        /// <summary>
        /// Releases the <see cref="Body"/> property.
        /// Nothing is done if body is null.
        /// </summary>
        /// <returns>a task representing the asynchronous release operation</returns>
        public Task Release()
        {
            var endReadTask = Body?.Release();
            return endReadTask ?? Task.CompletedTask;
        }
    }
}
