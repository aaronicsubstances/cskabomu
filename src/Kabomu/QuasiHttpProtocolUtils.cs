using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu
{
    /// <summary>
    /// Contains constants and helper functions involved in implementing quasi
    /// web protocol.
    /// </summary>
    public static class QuasiHttpProtocolUtils
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
        /// Environment variable indicating that the raw body 
        /// to be sent to transport should be transferred as is, without
        /// any encoding applied.
        /// </summary>
        public static readonly string EnvKeySkipRawBodyEncoding = "kabomu.skip_raw_body_encoding";

        /// <summary>
        /// Environment variable indicating that the raw body 
        /// to be received from transport should be returned as is, without
        /// any decoding applied.
        /// </summary>
        public static readonly string EnvKeySkipRawBodyDecoding = "kabomu.skip_raw_body_decoding";

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
        public const int DefaultMaxHeadersSize = 65_536;

        /// <summary>
        /// The number of ASCII bytes which indicate the length of
        /// the entire request/response header part.
        /// </summary>
        public const int LengthOfEncodedHeadersLength = 6;

        /// <summary>
        /// The maximum possible size that headers in a request or response
        /// cannot exceed.
        /// </summary>
        private const int HardLimitOnMaxHeadersSize = 999_999;

        /// <summary>
        /// The number of ASCII bytes which indicate the length of
        /// data in a body chunk.
        /// </summary>
        private const int LengthOfEncodedBodyChunkLength = 10;

        /// <summary>
        /// The maximum allowable body chunk data length.
        /// </summary>
        private const int MaxBodyChunkLength = 2_000_000_000;

        /// <summary>
        /// First version of quasi web protocol.
        /// </summary>
        public const string ProtocolVersion01 = "01";

        public static  byte[] EncodeRequestHeaders(
            IQuasiHttpRequest reqHeaders,
            int? maxHeadersSize = null)
        {
            var uniqueRow = new List<string>
            {
                reqHeaders.HttpMethod ?? "",
                reqHeaders.Target ?? "",
                reqHeaders.HttpVersion ?? "",
                reqHeaders.ContentLength.ToString()
            };
            return EncodeRemainingHeaders(uniqueRow,
                reqHeaders.Headers, maxHeadersSize ?? 0);
        }

        public static byte[] EncodeResponseHeaders(
            IQuasiHttpResponse resHeaders,
            int? maxHeadersSize = null)
        {
            var uniqueRow = new List<string>
            {
                resHeaders.StatusCode.ToString(),
                resHeaders.HttpStatusMessage ?? "",
                resHeaders.HttpVersion ?? "",
                resHeaders.ContentLength.ToString()
            };
            return EncodeRemainingHeaders(uniqueRow,
                resHeaders.Headers, maxHeadersSize ?? 0);
        }

        private static byte[] EncodeRemainingHeaders(List<string> uniqueRow, IDictionary<string, IList<string>> headers,
            int maxHeadersSize)
        {
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = DefaultMaxHeadersSize;
            }
            var csv = new List<IList<string>>();
            csv.Add(new List<string>
            {
                MiscUtils.PadLeftWithZeros("", LengthOfEncodedHeadersLength),
                ProtocolVersion01
            });
            csv.Add(uniqueRow);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Value == null || header.Value.Count == 0)
                    {
                        continue;
                    }
                    var headerRow = new List<string> { header.Key ?? "" };
                    foreach (var v in header.Value)
                    {
                        headerRow.Add(v ?? "");
                    }
                    csv.Add(headerRow);
                }
            }

            // serialize to bytes and check that byte count to encode
            // doesn't exceed limit.
            // NB: byte count to encode excludes length in leading row.
            var encoded = MiscUtils.StringToBytes(CsvUtils.Serialize(csv));
            var lengthToEncode = encoded.Length - LengthOfEncodedHeadersLength;
            if (lengthToEncode > maxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http headers exceed " +
                    $"max size ({lengthToEncode} > {maxHeadersSize})");
            }
            if (lengthToEncode > HardLimitOnMaxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http headers too " +
                    $"large ({lengthToEncode} > {HardLimitOnMaxHeadersSize})");
            }

            // finally update leading CSV value with byte count to
            // encode
            var encodedLength = MiscUtils.StringToBytes(
                MiscUtils.PadLeftWithZeros(lengthToEncode.ToString(),
                LengthOfEncodedHeadersLength));
            MiscUtils.ArrayCopy(encodedLength, 0, encoded, 0,
                encodedLength.Length);
            return encoded;
        }

        public static void DecodeRequestHeaders(byte[] encodedCsv,
            IQuasiHttpRequest request)
        {
            var csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                encodedCsv));
            if (csv.Count < 2)
            {
                throw new ArgumentException("invalid encoded quasi http request headers");
            }
            // skip first row.
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new ArgumentException("invalid quasi http request line");
            }
            request.HttpMethod = specialHeader[0];
            request.Target = specialHeader[1];
            request.HttpVersion = specialHeader[2];
            try
            {
                request.ContentLength = MiscUtils.ParseInt48(
                    specialHeader[3]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http request content length");
            }
            request.Headers = DecodeRemainingHeaders(csv);
        }

        public static void DecodeResponseHeaders(byte[] encodedCsv,
            IQuasiHttpResponse response)
        {
            var csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                encodedCsv));
            if (csv.Count < 2)
            {
                throw new ArgumentException("invalid encoded quasi http response headers");
            }
            // skip first row.
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new ArgumentException("invalid quasi http response status line");
            }
            try
            {
                response.StatusCode = MiscUtils.ParseInt32(specialHeader[0]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http response status code");
            }
            response.HttpStatusMessage = specialHeader[1];
            response.HttpVersion = specialHeader[2];
            try
            {
                response.ContentLength = MiscUtils.ParseInt48(specialHeader[3]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http response content length");
            }
            response.Headers = DecodeRemainingHeaders(csv);
        }

        private static IDictionary<string, IList<string>> DecodeRemainingHeaders(
            IList<IList<string>> csv)
        {
            IDictionary<string, IList<string>> headers = null;
            for (int i = 1; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                if (headers == null)
                {
                    headers = new Dictionary<string, IList<string>>();
                }
                // merge headers with the same name in different rows.
                string headerName = headerRow[0];
                if (!headers.ContainsKey(headerName))
                {
                    headers.Add(headerName, new List<string>());
                }
                var headerValues = headers[headerName];
                foreach (var headerValue in headerRow.Skip(1))
                {
                    headerValues.Add(headerValue);
                }
            }
            return headers;
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

        internal static bool DetermineEffectiveBooleanOption(
            bool? preferred, bool? fallback1, bool defaultValue)
        {
            if (preferred != null)
            {
                return preferred.Value;
            }
            return fallback1 ?? defaultValue;
        }

        public static bool? GetEnvVarAsBoolean(IDictionary<string, object> environment,
            string key)
        {
            if (environment != null && environment.ContainsKey(key))
            {
                var value = environment[key];
                if (value is bool b)
                {
                    return b;
                }
                else if (value != null)
                {
                    return bool.Parse((string)value);
                }
            }
            return null;
        }

        internal static Stream EncodeBodyToTransport(
            long contentLength, Stream body,
            IDictionary<string, object> environment)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (GetEnvVarAsBoolean(environment, EnvKeySkipRawBodyEncoding) == true)
            {
                return body;
            }
            if (contentLength < 0)
            {
                body = CreateCustomChunkEncodingStream(body);
            }
            else
            {
                // don't enforce content length during writes to transport.
            }
            return body;
        }

        internal static Stream DecodeBodyFromTransport(
            long contentLength, Stream body,
            IDictionary<string, object> environment)
        {
            if (contentLength == 0)
            {
                return null;
            }
            if (GetEnvVarAsBoolean(environment, EnvKeySkipRawBodyDecoding) == true)
            {
                return body;
            }
            if (contentLength < 0)
            {
                body = CreateCustomChunkDecodingStream(body);
            }
            else
            {
                body = CreateContentLengthEnforcingStream(body, contentLength);
            }
            return body;
        }

        public static Stream CreateContentLengthEnforcingStream(Stream backingStream,
            long contentLength)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            var backingGenerator = MiscUtils.CreateGeneratorFromInputStream(
                backingStream);
            return MiscUtils.CreateInputStreamFromGenerator(GenerateChunksForContentLengthEnforcement(
                backingGenerator, contentLength));
        }

        private static async IAsyncEnumerable<byte[]> GenerateChunksForContentLengthEnforcement(
            IAsyncEnumerable<byte[]> backingGenerator, long contentLength)
        {
            long bytesLeft = contentLength;
            // allow zero content length to touch backing stream.
            await foreach (var chunk in backingGenerator)
            {
                if (contentLength < 0)
                {
                    yield return chunk;
                }
                else if (chunk.Length < bytesLeft)
                {
                    yield return chunk;
                    bytesLeft -= chunk.Length;
                }
                else
                {
                    if (chunk.Length > bytesLeft)
                    {
                        var truncatedChunk = new byte[bytesLeft];
                        MiscUtils.ArrayCopy(chunk, 0,
                            truncatedChunk, 0, (int)bytesLeft);
                        yield return truncatedChunk;
                    }
                    else if (bytesLeft > 0) // caters for content length = 0
                    {
                        yield return chunk;
                    }
                    bytesLeft = 0;
                    break;
                }
            }
            if (contentLength > 0 && bytesLeft > 0)
            {
                throw CustomIOException.CreateContentLengthNotSatisfiedError(
                    contentLength, bytesLeft);
            }
        }

        public static Stream CreateCustomChunkEncodingStream(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            var backingGenerator = MiscUtils.CreateGeneratorFromInputStream(
                backingStream);
            return MiscUtils.CreateInputStreamFromGenerator(
                GenerateChunksForChunkEncoding(backingGenerator));
        }

        private static async IAsyncEnumerable<byte[]> GenerateChunksForChunkEncoding(
            IAsyncEnumerable<byte[]> backingGenerator)
        {
            await foreach (var chunk in backingGenerator)
            {
                var bodyChunks = GenerateBodyChunksV1(chunk);
                foreach (var bodyChunk in bodyChunks)
                {
                    yield return bodyChunk;
                }
            }
            yield return GenerateTerminatingBodyChunkV1();
        }

        public static Stream CreateCustomChunkDecodingStream(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            var backingGenerator = MiscUtils.CreateGeneratorFromInputStream(
                backingStream);
            return MiscUtils.CreateInputStreamFromGenerator(
                GenerateChunksForChunkDecoding(backingGenerator));
        }

        private static async IAsyncEnumerable<byte[]> GenerateChunksForChunkDecoding(
            IAsyncEnumerable<byte[]> backingGenerator)
        {
            var chunks = new List<byte[]>();
            var temp = new int[2];
            bool isDecodingHeader = true;
            int outstandingDataLength = 0;
            await foreach (var chunk in backingGenerator)
            {
                if (!isDecodingHeader)
                {
                    var chunkLengthToUse = Math.Min(outstandingDataLength,
                        chunk.Length);
                    if (chunkLengthToUse > 0)
                    {
                        var nextChunk = new byte[chunkLengthToUse];
                        MiscUtils.ArrayCopy(chunk, 0,
                            nextChunk, 0, nextChunk.Length);
                        yield return nextChunk;
                        outstandingDataLength -= chunkLengthToUse;
                    }
                    if (chunkLengthToUse < chunk.Length)
                    {
                        var carryOverChunk = new byte[chunk.Length - chunkLengthToUse];
                        MiscUtils.ArrayCopy(chunk, chunkLengthToUse,
                            carryOverChunk, 0, carryOverChunk.Length);
                        chunks.Add(carryOverChunk);
                        isDecodingHeader = true;
                        // proceed to loop
                    }
                    else
                    {
                        if (outstandingDataLength > 0)
                        {
                            // need to read more chunks to fulfil
                            // chunk data length.
                            continue;
                        }
                        else
                        {
                            // chunk exactly fulfilled outstanding
                            // data length.
                            isDecodingHeader = true;
                            // continue or proceed to loop,
                            // it doesn't matter, as chunks should
                            // be empty.
                        }
                    }
                }
                else
                {
                    chunks.Add(chunk);
                }
                bool endOfGenerator = false;
                while (true)
                {
                    byte[] concatenated;
                    try
                    {
                        concatenated = TryDecodeBodyChunkV1Header(chunks, temp);
                    }
                    catch (Exception e)
                    {
                        throw new ChunkDecodingException("Failed to decode quasi http body while " +
                            "reading body chunk header", e);
                    }
                    if (concatenated == null)
                    {
                        // need to read more chunks to fulfil
                        // chunk header length.
                        break;
                    }
                    chunks.Clear();
                    outstandingDataLength = temp[0];
                    if (outstandingDataLength == 0)
                    {
                        // end of generator detected.
                        endOfGenerator = true;
                        break;
                    }
                    int concatenatedLengthUsed = temp[1];
                    var nextChunkLength = Math.Min(outstandingDataLength,
                        concatenated.Length - concatenatedLengthUsed);
                    if (nextChunkLength > 0)
                    {
                        var nextChunk = new byte[nextChunkLength];
                        MiscUtils.ArrayCopy(concatenated, concatenatedLengthUsed,
                            nextChunk, 0, nextChunk.Length);
                        yield return nextChunk;
                        outstandingDataLength -= nextChunkLength;
                        concatenatedLengthUsed += nextChunkLength;
                    }
                    if (concatenatedLengthUsed < concatenated.Length)
                    {
                        // can't read more chunks yet, because there are
                        // more stuff inside concatenated
                        var carryOverChunk = new byte[concatenated.Length - concatenatedLengthUsed];
                        MiscUtils.ArrayCopy(concatenated, concatenatedLengthUsed,
                            carryOverChunk, 0, carryOverChunk.Length);
                        chunks.Add(carryOverChunk);
                    }
                    else
                    {
                        if (outstandingDataLength > 0)
                        {
                            // need to read more chunks to fulfil
                            // chunk data length.
                            isDecodingHeader = false;
                        }
                        else
                        {
                            // chunk exactly fulfilled outstanding
                            // data length.
                            // So start decoding header again.
                        }
                        // in any case need to read more chunks.
                        break;
                    }
                }
                if (endOfGenerator)
                {
                    break;
                }
            }
            if (isDecodingHeader && chunks.Count > 0)
            {
                throw new ChunkDecodingException(
                    "Failed to decode quasi http body while " +
                    "reading body chunk header: unexpected end of read");
            }
            if (!isDecodingHeader && outstandingDataLength > 0)
            {
                throw new ChunkDecodingException(
                    "Failed to decode quasi http body while " +
                    "reading body chunk data: unexpected end of read");
            }
        }

        private static byte[] TryDecodeBodyChunkV1Header(
            List<byte[]> chunks, int[] result)
        {
            // account for length of version 1 and separating comma.
            var minimumBodyChunkV1HeaderLength = LengthOfEncodedBodyChunkLength +
                ProtocolVersion01.Length + 1;
            int totalLength = MiscUtils.ComputeLengthOfBuffers(chunks);
            if (totalLength < minimumBodyChunkV1HeaderLength)
            {
                return null;
            }
            var decodingBuffer = MiscUtils.ConcatBuffers(chunks);
            var csv = CsvUtils.Deserialize(MiscUtils.BytesToString(
                decodingBuffer, 0, minimumBodyChunkV1HeaderLength));
            if (csv.Count == 0)
            {
                throw new ArgumentException("invalid quasi http body chunk header");
            }
            var specialHeader = csv[0];
            if (specialHeader.Count < 2)
            {
                throw new ArgumentException("invalid quasi http body chunk header");
            }
            // validate version column as valid positive integer.
            int v;
            try
            {
                v = MiscUtils.ParseInt32(specialHeader[0]);
            }
            catch (FormatException)
            {
                throw new ArgumentException("invalid version field: " + specialHeader[0]);
            }
            if (v <= 0)
            {
                throw new ArgumentException("invalid nonnegative version number: " + v);
            }
            var lengthOfData = MiscUtils.ParseInt32(specialHeader[1]);
            if (lengthOfData < 0)
            {
                throw new ArgumentException("invalid quasi http body chunk length " +
                    $"({lengthOfData})");
            }
            result[0] = lengthOfData;
            result[1] = minimumBodyChunkV1HeaderLength;
            return decodingBuffer;
        }

        private static byte[] EncodeBodyChunkV1Header(int length)
        {
            var csv = ProtocolVersion01 + ",";
            csv += MiscUtils.PadLeftWithZeros(
                length.ToString(),
                LengthOfEncodedBodyChunkLength);
            return MiscUtils.StringToBytes(csv);
        }

        public static byte[] GenerateTerminatingBodyChunkV1()
        {
            return EncodeBodyChunkV1Header(0);
        }

        public static List<byte[]> GenerateBodyChunksV1(byte[] data)
        {
            return GenerateBodyChunksV1(data, 0, data.Length);
        }

        public static List<byte[]> GenerateBodyChunksV1(
            byte[] data, int offset, int length)
        {
            var chunks = new List<byte[]>();
            int endOffset = offset + length;
            while (offset < endOffset)
            {
                offset += GenerateBodyChunksV1Internal(data,
                    offset, endOffset - offset,
                    chunks);
            }
            return chunks;
        }

        private static int GenerateBodyChunksV1Internal(byte[] data,
            int offset, int length, List<byte[]> chunks)
        {
            int bytesToRead = Math.Min(length, MaxBodyChunkLength);
            var encodedLength = EncodeBodyChunkV1Header(bytesToRead);
            var chunk = new byte[encodedLength.Length + bytesToRead];
            MiscUtils.ArrayCopy(encodedLength, 0, chunk, 0,
                encodedLength.Length);
            MiscUtils.ArrayCopy(data, offset, chunk, encodedLength.Length,
                bytesToRead);
            chunks.Add(chunk);
            return bytesToRead;
        }
    }
}