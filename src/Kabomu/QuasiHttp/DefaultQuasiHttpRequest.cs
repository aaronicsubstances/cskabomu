using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Provides convenient implementation of <see cref="IQuasiHttpRequest"/> and
    /// <see cref="IQuasiHttpMutableRequest"/> interfaces.
    /// </summary>
    public class DefaultQuasiHttpRequest : IQuasiHttpMutableRequest
    {
        /// <summary>
        /// Equals HTTP method "GET".
        /// </summary>
        public static readonly string MethodGet = "GET";

        /// <summary>
        /// Equals HTTP method "POST".
        /// </summary>
        public static readonly string MethodPost = "POST";

        /// <summary>
        /// Equals HTTP method "PUT".
        /// </summary>
        public static readonly string MethodPut = "PUT";

        /// <summary>
        /// Equals HTTP method "DELETE".
        /// </summary>
        public static readonly string MethodDelete = "DELETE";

        /// <summary>
        /// Equals HTTP method "HEAD".
        /// </summary>
        public static readonly string MethodHead = "HEAD";

        /// <summary>
        /// Equals HTTP method "OPTIONS".
        /// </summary>
        public static readonly string MethodOptions = "OPTIONS";

        /// <summary>
        /// Equals HTTP method "PATCH".
        /// </summary>
        public static readonly string MethodPatch = "PATCH";

        /// <summary>
        /// Equals HTTP method "TRACE".
        /// </summary>
        public static readonly string MethodTrace = "TRACE";

        /// <summary>
        /// Equals HTTP method "CONNECT".
        /// </summary>
        public static readonly string MethodConnect = "CONNECT";

        /// <summary>
        /// Gets or sets the equivalent of request target component of HTTP request line.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request headers.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, headers are case sensitive. Also setting a Content-Length header
        /// here will have no bearing on how to transmit or receive the request body.
        /// </remarks>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Gets or sets the request body.
        /// </summary>
        public IQuasiHttpBody Body { get; set; }

        /// <summary>
        /// Gets or sets an HTTP method value.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets an HTTP request version value.
        /// </summary>
        public string HttpVersion { get; set; }

        /// Gets or sets any objects which may be of interest to code
        /// which will process this request instance.
        public IDictionary<string, object> Environment { get; set; }

        /// <summary>
        /// Releases the Body property.
        /// </summary>
        /// <returns>a task representing the asynchronous release operation</returns>
        public Task Release()
        {
            var endReadTask = Body?.Release();
            return endReadTask ?? Task.CompletedTask;
        }
    }
}
