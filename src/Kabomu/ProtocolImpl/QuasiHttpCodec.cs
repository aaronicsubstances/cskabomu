using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static readonly int TagForHeaders = 0x71683031;

        public static readonly int TagForBody = 0x71623031;

        /// <summary>
        /// Serializes quasi http request or response headers.
        /// </summary>
        /// <param name="reqOrStatusLine">request or response status line</param>
        /// <param name="remainingHeaders">headers after request or status line</param>
        /// <returns>serialized representation of quasi http request headers</returns>
        public static byte[] EncodeQuasiHttpHeaders(
            IList<string> reqOrStatusLine,
            IDictionary<string, IList<string>> remainingHeaders)
        {
            var csv = new List<IList<string>>();
            var specialHeader = new List<string>();
            foreach (var v in reqOrStatusLine)
            {
                specialHeader.Add(v ?? "");
            }
            csv.Add(specialHeader);
            if (remainingHeaders != null)
            {
                foreach (var header in remainingHeaders)
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

            var serialized = MiscUtilsInternal.StringToBytes(
                CsvUtils.Serialize(csv));

            return serialized;
        }

        /// <summary>
        /// Deserializes a quasi http request or response header section.
        /// </summary>
        /// <param name="data">source of data to deserialize</param>
        /// <param name="offset">starting position in buffer to start
        /// deserializing from</param>
        /// <param name="length">number of bytes to deserialize</param>
        /// <param name="headersReceiver">will be extended with remaining headers found
        /// after the request or response line</param>
        /// <returns>request or response line, ie first row before headers</returns>
        /// <exception cref="QuasiHttpException">if byte slice argument contains
        /// invalid quasi http request or response headers</exception>
        public static IList<string> DecodeQuasiHttpHeaders(
            byte[] data, int offset, int length,
            IDictionary<string, IList<string>> headersReceiver)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (!MiscUtilsInternal.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid byte buffer slice");
            }
            IList<IList<string>> csv;
            try
            {
                csv = CsvUtils.Deserialize(MiscUtilsInternal.BytesToString(
                    data, offset, length));
            }
            catch (Exception e)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http headers",
                    QuasiHttpException.ReasonCodeProtocolViolation,
                    e);
            }
            if (csv.Count == 0)
            {
                throw new QuasiHttpException(
                    $"invalid quasi http headers",
                    QuasiHttpException.ReasonCodeProtocolViolation);
            }
            var specialHeader = csv[0];

            // merge headers with the same name in different rows.
            for (int i = 1; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                string headerName = headerRow[0];
                if (!headersReceiver.ContainsKey(headerName))
                {
                    headersReceiver.Add(headerName, new List<string>());
                }
                var headerValues = headersReceiver[headerName];
                foreach (var headerValue in headerRow.Skip(1))
                {
                    headerValues.Add(headerValue);
                }
            }

            return specialHeader;
        }

        /// <summary>
        /// Serializes quasi http request or response header section to a writable
        /// stream.
        /// </summary>
        /// <param name="dest">writable stream to which serialized quasi http request or
        /// response section will be written</param>
        /// <param name="reqOrStatusLine">request or response status line</param>
        /// <param name="remainingHeaders">headers after request or status line</param>
        /// <param name="maxHeadersSize">limit on size of serialized result.
        /// Can be zero for a default value to be used.</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>task represent asynchronous operation</returns>
        /// <exception cref="QuasiHttpException">if serialized is too large</exception>
        public static async Task WriteQuasiHttpHeaders(
            Stream dest,
            IList<string> reqOrStatusLine,
            IDictionary<string, IList<string>> remainingHeaders,
            int maxHeadersSize = 0,
            CancellationToken cancellationToken = default)
        {
            var encodedHeaders = EncodeQuasiHttpHeaders(reqOrStatusLine,
                remainingHeaders);
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }

            // finally check that byte count of csv doesn't exceed limit.
            if (encodedHeaders.Length > maxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({encodedHeaders.Length} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            await dest.WriteAsync(TlvUtils.EncodeTagAndLengthOnly(TagForHeaders,
                encodedHeaders.Length), cancellationToken);
            await dest.WriteAsync(encodedHeaders,
                cancellationToken);
        }

        public static async Task<IList<string>> ReadQuasiHttpHeaders(
            Stream src,
            IDictionary<string, IList<string>> headersReceiver,
            int maxHeadersSize = 0,
            CancellationToken cancellationToken = default)
        {
            await TlvUtils.ReadExpectedTagOnly(src, TagForHeaders,
                cancellationToken);
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = QuasiHttpUtils.DefaultMaxHeadersSize;
            }
            int headersSize = await TlvUtils.ReadLengthOnly(
                src, cancellationToken);
            if (headersSize > maxHeadersSize)
            {
                throw new QuasiHttpException("quasi http headers exceed " +
                    $"max size ({headersSize} > {maxHeadersSize})",
                    QuasiHttpException.ReasonCodeMessageLengthLimitExceeded);
            }
            var encodedHeaders = new byte[headersSize];
            await IOUtilsInternal.ReadBytesFully(src,
                encodedHeaders, 0, encodedHeaders.Length,
                cancellationToken);
            return DecodeQuasiHttpHeaders(encodedHeaders, 0, encodedHeaders.Length,
                headersReceiver);
        }
    }
}