using Kabomu.Abstractions;
using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// The standard encoder and decoder of quasi http request and response
    /// headers in the Kabomu library.
    /// </summary>
    public static class QuasiHttpCodec
    {
        /// <summary>
        /// This field gives a number of which all header sizes are
        /// an integral multiple of.
        /// </summary>
        internal const int HeaderChunkSize = 512;

        /// <summary>
        /// First version of quasi web protocol.
        /// </summary>
        internal const string ProtocolVersion01 = "01";

        /// <summary>
        /// Serializes quasi http request headers.
        /// </summary>
        /// <param name="reqHeaders">source of quasi http request headers</param>
        /// <param name="maxHeadersSize">limit on size of serialized result.
        /// Can be null or zero for a default value to be used.</param>
        /// <returns>serialized representation of quasi http request headers</returns>
        public static byte[] EncodeRequestHeaders(
            IQuasiHttpRequest reqHeaders,
            int? maxHeadersSize = null)
        {
            if (reqHeaders == null)
            {
                throw new ArgumentNullException(nameof(reqHeaders));
            }
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

        /// <summary>
        /// Serializes quasi http response headers.
        /// </summary>
        /// <param name="resHeaders">source of quasi http response headers</param>
        /// <param name="maxHeadersSize">limit on size of serialized result.
        /// Can be null or zero for a default value to be used.</param>
        /// <returns>serialized representation of quasi http response headers</returns>
        public static byte[] EncodeResponseHeaders(
            IQuasiHttpResponse resHeaders,
            int? maxHeadersSize = null)
        {
            if (resHeaders == null)
            {
                throw new ArgumentNullException(nameof(resHeaders));
            }
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
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }
            var csv = new List<IList<string>>();
            csv.Add(new List<string>
            {
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

            // ensure there are no new lines in csv items
            if (csv.Any(row => row.Any(item => item.Contains('\n') ||
                item.Contains('\r'))))
            {
                throw new QuasiHttpException("quasi http headers cannot " +
                    "contain newlines",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }

            // add at least two line feeds to ensure byte count
            // is multiple of header chunk size.
            var serialized = CsvUtils.Serialize(csv);
            var effectiveByteCount = MiscUtilsInternal.GetByteCount(serialized);
            var lfCount = (int)Math.Ceiling(effectiveByteCount /
                (double)HeaderChunkSize) * HeaderChunkSize -
                effectiveByteCount;
            if (lfCount < 2)
            {
                lfCount += HeaderChunkSize;
            }
            serialized += "".PadRight(lfCount, '\n');
            effectiveByteCount += lfCount;

            // finally check that byte count of csv doesn't exceed limit.
            if (effectiveByteCount > maxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({effectiveByteCount} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            return MiscUtilsInternal.StringToBytes(serialized);
        }

        /// <summary>
        /// Deserializes a quasi http request header section.
        /// </summary>
        /// <param name="data">source of data to deserialize</param>
        /// <param name="offset">starting position in buffer to start
        /// deserializing from</param>
        /// <param name="length">number of bytes to deserialize</param>
        /// <param name="request">object whose header-related properties will be
        /// set with decoded quasi http request headers</param>
        /// <exception cref="QuasiHttpException">if byte slice argument contains
        /// invalid quasi http request headers</exception>
        public static void DecodeRequestHeaders(
            byte[] data, int offset, int length,
            IQuasiHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            IList<IList<string>> csv = StartDecodeReqOrRes(
                data, offset, length, false);
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpException(
                    "invalid quasi http request line",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            request.HttpMethod = specialHeader[0];
            request.Target = specialHeader[1];
            request.HttpVersion = specialHeader[2];
            try
            {
                request.ContentLength = MiscUtilsInternal.ParseInt48(
                    specialHeader[3]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    "invalid quasi http request content length",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            request.Headers = DecodeRemainingHeaders(csv);
        }

        /// <summary>
        /// Deserializes a quasi http response header section.
        /// </summary>
        /// <param name="data">source of data to deserialize</param>
        /// <param name="offset">starting position in buffer to start
        /// deserializing from</param>
        /// <param name="length">number of bytes to deserialize</param>
        /// <param name="response">object whose header-related properties will be
        /// set with decoded quasi http response headers</param>
        /// <exception cref="QuasiHttpException">if byte slice argument contains
        /// invalid quasi http response headers</exception>
        public static void DecodeResponseHeaders(
            byte[] data, int offset, int length,
            IQuasiHttpResponse response)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }
            IList<IList<string>> csv = StartDecodeReqOrRes(
                data, offset, length, true);
            var specialHeader = csv[1];
            if (specialHeader.Count < 4)
            {
                throw new QuasiHttpException(
                    "invalid quasi http status line",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            try
            {
                response.StatusCode = MiscUtilsInternal.ParseInt32(specialHeader[0]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    "invalid quasi http response status code",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            response.HttpStatusMessage = specialHeader[1];
            response.HttpVersion = specialHeader[2];
            try
            {
                response.ContentLength = MiscUtilsInternal.ParseInt48(specialHeader[3]);
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    "invalid quasi http response content length",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            response.Headers = DecodeRemainingHeaders(csv);
        }

        private static IList<IList<string>> StartDecodeReqOrRes(
            byte[] data, int offset, int length, bool isResponse)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (!MiscUtilsInternal.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid byte buffer slice");
            }
            string tag = isResponse ? "response" : "request";
            IList<IList<string>> csv;
            try
            {
                csv = CsvUtils.Deserialize(MiscUtilsInternal.BytesToString(
                    data, offset, length));
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http {tag} headers",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            if (csv.Count < 2 || csv[0].Count == 0 ||
                csv[0][0] != ProtocolVersion01)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http {tag} headers",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            return csv;
        }

        private static IDictionary<string, IList<string>> DecodeRemainingHeaders(
            IList<IList<string>> csv)
        {
            var headers = new Dictionary<string, IList<string>>();
            for (int i = 2; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
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
    }
}