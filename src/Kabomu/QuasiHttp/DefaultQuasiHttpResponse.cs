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
        /// Status code value of 200, equivalent to HTTP status code 200 OK.
        /// </summary>
        public static readonly int StatusCodeOk = 200;

        /// <summary>
        /// Status code value of 400, equivalent to HTTP status code 400 Bad Request.
        /// </summary>
        /// <remarks>
        /// Intended to be used when one does not want to be more specific about the particular client error.
        /// </remarks>
        public static readonly int StatusCodeClientError = 400;

        /// <summary>
        /// Status code value of 400, equivalent to HTTP status code 400 Bad Request.
        /// </summary>
        public static readonly int StatusCodeClientErrorBadRequest = 400;

        /// <summary>
        /// Status code value of 401, equivalent to HTTP status code 401 Unauthorized.
        /// </summary>
        public static readonly int StatusCodeClientErrorUnauthorized = 401;

        /// <summary>
        /// Status code value of 403, equivalent to HTTP status code 403 Forbidden.
        /// </summary>
        public static readonly int StatusCodeClientErrorForbidden = 403;

        /// <summary>
        /// Status code value of 404, equivalent to HTTP status code 404 Not Found.
        /// </summary>
        public static readonly int StatusCodeClientErrorNotFound = 404;

        /// <summary>
        /// Status code value of 405, equivalent to HTTP status code 405 Method Not Allowed.
        /// </summary>
        public static readonly int StatusCodeClientErrorMethodNotAllowed = 405;

        /// <summary>
        /// Status code value of 413, equivalent to HTTP status code 413 Payload Too Large.
        /// </summary>
        public static readonly int StatusCodeClientErrorPayloadTooLarge = 413;

        /// <summary>
        /// Status code value of 414, equivalent to HTTP status code 414 URI Too Long.
        /// </summary>
        public static readonly int StatusCodeClientErrorURITooLong = 414;

        /// <summary>
        /// Status code value of 415, equivalent to HTTP status code 415 Unsupported Media Type.
        /// </summary>
        public static readonly int StatusCodeClientErrorUnsupportedMediaType = 415;

        /// <summary>
        /// Status code value of 422, equivalent to HTTP status code 422 Unprocessable Entity.
        /// </summary>
        public static readonly int StatusCodeClientErrorUnprocessableEntity = 422;

        /// <summary>
        /// Status code value of 429, equivalent to HTTP status code 429 Too Many Requests.
        /// </summary>
        public static readonly int StatusCodeClientErrorTooManyRequests = 429;

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
        /// Gets or sets any objects which may be of interest to the code which
        /// will process this response instance.
        /// </summary>
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
