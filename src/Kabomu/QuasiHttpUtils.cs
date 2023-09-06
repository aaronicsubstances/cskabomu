using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// Provides helper functions for common operations performed by
    /// during custom chunk protocol processing.
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
        /// Environment variable for indicating that a request or response
        /// should not be sent at all. Intended
        /// for use in responding to fire and forget requests, as well as
        /// cases where request or response has been sent already by other
        /// means.
        /// </summary>
        public static readonly string EnvKeySkipSending = "kabomu.skip_sending";

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

        /// <summary>
        /// The limit of data buffering when reading byte streams into memory. Equal to 128 MB.
        /// </summary>
        public static readonly int DefaultDataBufferLimit = 134_217_728;

        /// <summary>
        /// The default read buffer size. Equal to 8,192 bytes.
        /// </summary>
        private static readonly int DefaultReadBufferSize = 8_192;

        /// <summary>
        /// Performs writes on behalf of an instance of <see cref="Stream"/>
        /// class or <see cref="ICustomWriter"/> interface.
        /// </summary>
        /// <param name="writer">instance of <see cref="Stream"/>
        /// class or <see cref="ICustomWriter"/> interface</param>
        /// <param name="data">source of bytes to be written</param>
        /// <param name="offset">starting position in buffer from which to
        /// fetch bytes to be written</param>
        /// <param name="length">number of bytes to write</param>
        /// <returns>a task representing end of asynchronous write operation</returns>
        /// <exception cref="ArgumentNullException">The <see cref="writer"/> argument
        /// is null</exception>
        public static Task WriteBytes(object writer,
            byte[] data, int offset, int length)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            // allow zero-byte writes to proceed to the
            // stream, rather than just return.
            if (writer is Stream s)
            {
                return s.WriteAsync(data, offset, length);
            }
            else
            {
                return ((ICustomWriter)writer).WriteBytes(data, offset, length);
            }
        }

        /// <summary>
        /// Performs reads on behalf of an instance of <see cref="Stream"/>
        /// class or <see cref="ICustomReader"/> interface.
        /// </summary>
        /// <param name="reader">instance of <see cref="Stream"/>
        /// class or <see cref="ICustomReader"/> interface</param>
        /// <param name="data">destination of bytes to be read</param>
        /// <param name="offset">starting position in buffer from which to begin
        /// storing read bytes</param>
        /// <param name="length">number of bytes to read</param>
        /// <returns>a task whose result will be the number of bytes actually read, which
        /// depending of the kind of reader may be less than the number of bytes requested.</returns>
        /// <exception cref="ArgumentNullException">The <see cref="reader"/> argument
        /// is null</exception>
        public static async Task<int> ReadBytes(object reader,
            byte[] data, int offset, int length)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            // allow zero-byte reads to proceed to touch the
            // stream, rather than just return 0.
            int bytesRead;
            if (reader is Stream s)
            {
                bytesRead = await s.ReadAsync(data, offset, length);
            }
            else
            {
                bytesRead = await ((ICustomReader)reader).ReadBytes(data, offset, length);
            }
            if (bytesRead > length)
            {
                throw new ExpectationViolationException(
                    "read beyond requested length: " +
                    $"({bytesRead} > {length})");
            }
            return bytesRead;
        }

        /// <summary>
        /// Reads in data in order to completely fill in a buffer.
        /// </summary>
        /// <param name="reader">source of bytes to read acceptable by
        /// <see cref="ReadBytes"/> function.</param>
        /// <param name="data">destination buffer</param>
        /// <param name="offset">start position in buffer to fill from</param>
        /// <param name="length">number of bytes to read. Failure to obtain this number of bytes will
        /// result in an error.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        /// <exception cref="CustomIOException">Not enough bytes were found in
        /// <see cref="reader"/> argument to supply requested number of bytes.</exception>
        public static async Task ReadBytesFully(object reader,
            byte[] data, int offset, int length)
        {
            // allow zero-byte reads to proceed to touch the
            // stream, rather than just return.
            while (true)
            {
                int bytesRead = await ReadBytes(reader, data, offset, length);
                if (bytesRead > length)
                {
                    throw new ExpectationViolationException(
                        "read beyond requested length: " +
                        $"({bytesRead} > {length})");
                }
                if (bytesRead < length)
                {
                    if (bytesRead <= 0)
                    {
                        throw new CustomIOException("unexpected end of read");
                    }
                    offset += bytesRead;
                    length -= bytesRead;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Reads in all of a reader's data into an in-memory buffer.
        /// </summary>
        /// <remarks>
        /// One can specify a maximum size beyond which an error will be
        /// thrown if there is more data after that limit.
        /// </remarks>
        /// <param name="reader">The source of data to read acceptable by
        /// <see cref="ReadBytes"/> function.</param>
        /// <param name="bufferingLimit">Indicates the maximum size in bytes of the resulting buffer.
        /// Can pass zero to use default value. Can also pass a negative value which will ignore
        /// imposing a maximum size.</param>
        /// <returns>A task whose result is an in-memory buffer which has all of the remaining data in the reader.</returns>
        /// <exception cref="CustomIOException">The <paramref name="bufferingLimit"/> argument indicates a positive value,
        /// and data in <paramref name="body"/> argument exceeded that value.</exception>
        public static async Task<byte[]> ReadAllBytes(object reader,
            int bufferingLimit = 0)
        {
            if (bufferingLimit == 0)
            {
                bufferingLimit = DefaultDataBufferLimit;
            }

            var byteStream = new MemoryStream();
            if (bufferingLimit < 0)
            {
                await CopyBytes(reader, byteStream);
                return byteStream.ToArray();
            }

            var readBuffer = new byte[DefaultReadBufferSize];
            int totalBytesRead = 0;

            while (true)
            {
                int bytesToRead = Math.Min(readBuffer.Length, bufferingLimit - totalBytesRead);
                // force a read of 1 byte if there are no more bytes to read into memory stream buffer
                // but still remember that no bytes was expected.
                var expectedEndOfRead = false;
                if (bytesToRead == 0)
                {
                    bytesToRead = 1;
                    expectedEndOfRead = true;
                }
                int bytesRead = await ReadBytes(reader, readBuffer, 0, bytesToRead);
                if (bytesRead > 0)
                {
                    if (expectedEndOfRead)
                    {
                        throw CustomIOException.CreateDataBufferLimitExceededErrorMessage(
                            bufferingLimit);
                    }
                    byteStream.Write(readBuffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                }
                else
                {
                    break;
                }
            }
            return byteStream.ToArray();
        }

        /// <summary>
        /// Copies bytes from a reader to a writer.
        /// </summary>
        /// <param name="reader">source of data being transferred which is
        /// acceptable by <see cref="ReadBytes"/> function.</param>
        /// <param name="writer">destination of data being transferred
        /// which is acceptable by <see cref="WriteBytes"/> function</param>
        /// <returns>A task that represents the asynchronous copy operation.</returns>
        public static async Task CopyBytes(object reader, object writer)
        {
            if (reader is Stream r && writer is Stream w)
            {
                await r.CopyToAsync(w);
                return;
            }

            byte[] readBuffer = new byte[DefaultReadBufferSize];

            while (true)
            {
                int bytesRead = await ReadBytes(reader, readBuffer, 0, readBuffer.Length);

                if (bytesRead > 0)
                {
                    await WriteBytes(writer, readBuffer, 0, bytesRead);
                }
                else
                {
                    break;
                }
            }
        }

        public static object NormalizeReader(object body)
        {
            if (body is Stream s)
            {
                return s;
            }
            if (body is byte[] buffer)
            {
                return new MemoryStream(buffer);
            }
            if (body is string d)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(d));
            }
            if (body is IList<IList<string>> csv)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(
                    CsvUtils.Serialize(csv)));
            }
            return (ICustomReader)body;
        }

        /// <summary>
        /// Parses a string as a valid 48-bit signed integer.
        /// </summary>
        /// <param name="input">the string to parse. Can be surrounded by
        /// whitespace</param>
        /// <returns>verified 48-bit integer</returns>
        /// <exception cref="FormatException">if an error occurs</exception>
        public static long ParseInt48(string input)
        {
            var n = long.Parse(input);
            if (n < -140_737_488_355_328L || n > 140_737_488_355_327L)
            {
                throw new FormatException("invalid 48-bit integer: " + input);
            }
            return n;
        }

        public static IQuasiHttpProcessingOptions MergeProcessingOptions(
            IQuasiHttpProcessingOptions preferred,
            IQuasiHttpProcessingOptions fallback)
        {
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

            mergedOptions.ResponseBufferingEnabled =
                DetermineEffectiveBooleanOption(
                    preferred?.ResponseBufferingEnabled,
                    fallback?.ResponseBufferingEnabled,
                    true);

            mergedOptions.MaxHeadersSize =
                DetermineEffectivePositiveIntegerOption(
                    preferred?.MaxHeadersSize,
                    fallback?.MaxHeadersSize,
                    0);

            mergedOptions.ResponseBodyBufferingSizeLimit =
                DetermineEffectivePositiveIntegerOption(
                    preferred?.ResponseBodyBufferingSizeLimit,
                    fallback?.ResponseBodyBufferingSizeLimit,
                    0);
            return mergedOptions;
        }

        internal static int DetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue)
        {
            if (preferred.HasValue)
            {
                int effectiveValue = preferred.Value;
                if (effectiveValue != 0)
                {
                    return effectiveValue;
                }
            }
            if (fallback1.HasValue)
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
            if (preferred.HasValue)
            {
                int effectiveValue = preferred.Value;
                if (effectiveValue > 0)
                {
                    return effectiveValue;
                }
            }
            if (fallback1.HasValue)
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

        internal static bool DetermineEffectiveBooleanOption(bool? preferred, bool? fallback1, bool defaultValue)
        {
            if (preferred.HasValue)
            {
                return preferred.Value;
            }
            return fallback1 ?? defaultValue;
        }
    }
}
