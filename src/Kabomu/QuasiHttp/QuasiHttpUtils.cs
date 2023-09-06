using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    /// <summary>
    /// Contains names and values of common HTTP methods and status codes,
    /// as well as other constant values of use during quasi http request and response
    /// processing.
    /// </summary>
    public static class QuasiHttpUtils
    {
        /// <summary>
        /// Request environment variable for local server endpoint.
        /// </summary>
        public static readonly string ReqEnvKeyLocalPeerEndpoint = "kabomu.local_peer_endpoint";

        /// <summary>
        /// Request environment variable for remote client endpoint.
        /// </summary>
        public static readonly string EnvKeyRemotePeerEndpoint = "kabomu.remote_peer_endpoint";

        /// <summary>
        /// Request environment variable for the transport instance from which a request was received.
        /// </summary>
        public static readonly string EnvKeyTransportInstance = "kabomu.transport";

        /// <summary>
        /// Request environment variable for the connection from which a request was received.
        /// </summary>
        public static readonly string EnvKeyConnection = "kabomu.connection";

        /// <summary>
        /// Response environment variable indicating raw body received from transport
        /// without any decoding applied.
        /// </summary>
        public static readonly string EnvKeyRawBody = "kabomu.raw_body";

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
        /// Status code value of 200, equivalent to HTTP status code 200 OK.
        /// </summary>
        public static readonly int StatusCodeOk = 200;

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
    }
}
