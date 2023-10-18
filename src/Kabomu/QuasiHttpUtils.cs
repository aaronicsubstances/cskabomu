using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// Provides helper constants and functions
    /// that may be neeeded by clients of Kabomu library.
    /// </summary>
    public static class QuasiHttpUtils
    {
        /// <summary>
        /// Request environment variable for local server endpoint.
        /// </summary>
        public static readonly string EnvKeyLocalPeerEndpoint = "kabomu.local_peer_endpoint";

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

        /// <summary>
        /// The default value of maximum size of headers in a request or response.
        /// </summary>
        public const int DefaultMaxHeadersSize = 8_192;

        /// <summary>
        /// Merges two sources of processing options together, unless one of 
        /// them is null, in which case it returns the non-null one.
        /// </summary>
        /// <param name="preferred">options object whose valid property values will
        /// make it to merged result</param>
        /// <param name="fallback">options object whose valid property
        /// values will make it to merged result, if corresponding property
        /// on preferred argument are invalid.</param>
        /// <returns>merged options</returns>
        public static IQuasiHttpProcessingOptions MergeProcessingOptions(
            IQuasiHttpProcessingOptions preferred,
            IQuasiHttpProcessingOptions fallback)
        {
            if (preferred == null || fallback == null)
            {
                return preferred ?? fallback;
            }
            var mergedOptions = new DefaultQuasiHttpProcessingOptions();
            mergedOptions.TimeoutMillis =
                DetermineEffectiveNonZeroIntegerOption(
                    preferred?.TimeoutMillis,
                    fallback?.TimeoutMillis,
                    0);

            mergedOptions.ExtraConnectivityParams =
                DetermineEffectiveOptions(
                    preferred?.ExtraConnectivityParams,
                    fallback?.ExtraConnectivityParams);

            mergedOptions.MaxHeadersSize =
                DetermineEffectivePositiveIntegerOption(
                    preferred?.MaxHeadersSize,
                    fallback?.MaxHeadersSize,
                    0);

            mergedOptions.MaxResponseBodySize =
                DetermineEffectiveNonZeroIntegerOption(
                    preferred?.MaxResponseBodySize,
                    fallback?.MaxResponseBodySize,
                    0);
            return mergedOptions;
        }

        internal static int DetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue)
        {
            if (preferred != null)
            {
                int effectiveValue = preferred.Value;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            if (fallback1 != null)
            {
                int effectiveValue = fallback1.Value;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            return defaultValue;
        }

        internal static int DetermineEffectivePositiveIntegerOption(int? preferred,
            int? fallback1, int defaultValue)
        {
            if (preferred != null)
            {
                int effectiveValue = preferred.Value;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            if (fallback1 != null)
            {
                int effectiveValue = fallback1.Value;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            return defaultValue;
        }

        internal static IDictionary<string, object> DetermineEffectiveOptions(
            IDictionary<string, object> preferred, IDictionary<string, object> fallback)
        {
            var dest = new Dictionary<string, object>();
            // since we want preferred options to overwrite fallback options,
            // set fallback options first.
            if (fallback != null)
            {
                foreach (var item in fallback)
                {
                    dest.Add(item.Key, item.Value);
                }
            }
            if (preferred != null)
            {
                foreach (var item in preferred)
                {
                    if (dest.ContainsKey(item.Key))
                    {
                        dest[item.Key] = item.Value;
                    }
                    else
                    {
                        dest.Add(item.Key, item.Value);
                    }
                }
            }
            return dest;
        }

        /// <summary>
        /// Creates an object representing both a pending timeout and the
        /// function to cancel the timeout.
        /// </summary>
        /// <remarks>
        /// Note that if the timeout occurs, the pending task to be returned will
        /// complete normally with a result of true. If the timeout is cancelled,
        /// the task will still complete but with false result.
        /// </remarks>
        /// <param name="timeoutMillis">timeout value in milliseconds. must
        /// be positive for non-null object to be returned.</param>
        /// <returns>object that can be used to wait for pending timeout
        /// or cancel the pending timeout.</returns>
        public static ICancellableTimeoutTask CreateCancellableTimeoutTask(
            int timeoutMillis)
        {
            if (timeoutMillis <= 0)
            {
                return null;
            }
            var timeoutId = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeoutMillis, timeoutId.Token)
                .ContinueWith(t =>
                {
                    return !t.IsCanceled;
                });
            return new DefaultCancellableTimeoutTaskInternal
            {
                Task = timeoutTask,
                CancellationTokenSource = timeoutId
            };
        }
    }
}
