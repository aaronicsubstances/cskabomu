using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class QuasiHttpHeadersCodec
    {
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
        /// The maximum possible size that headers in a request or response cannot
        /// exceed.
        /// </summary>
        public const int HardLimitOnMaxHeadersSize = 999_999;

        public const int LengthOfEncodedBodyChunkLength = 10;
        public const int MaxBodyChunkLength = 1_000_000_000;

        private readonly byte[] DecodingBuffer = new byte[LengthOfEncodedBodyChunkLength];

        public byte[] EncodeRequestHeaders(
            IQuasiHttpRequestHeaderPart reqHeaders,
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

        public byte[] EncodeResponseHeaders(
            IQuasiHttpResponseHeaderPart resHeaders,
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

        private byte[] EncodeRemainingHeaders(List<string> uniqueRow, IDictionary<string, IList<string>> headers,
            int maxHeadersSize)
        {
            if (maxHeadersSize <= 0)
            {
                maxHeadersSize = DefaultMaxHeadersSize;
            }
            var csv = new List<IList<string>>();
            csv.Add(new List<string>
            {
                "".PadRight(LengthOfEncodedHeadersLength, '0'),
            });
            csv.Add(uniqueRow);
            int runningLengthEstimate = csv.Sum(x => x.Sum(y => y.Length));
            if (runningLengthEstimate > maxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http headers will exceed " +
                    $"max size (> {runningLengthEstimate} > {maxHeadersSize})");
            }
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    if (header.Value == null || header.Value.Count == 0)
                    {
                        continue;
                    }
                    var headerRow = new List<string> { header.Key ?? "" };
                    headerRow.AddRange(header.Value.Select(v => v ?? ""));
                    runningLengthEstimate += headerRow.Sum(x => x.Length);
                    if (runningLengthEstimate > maxHeadersSize)
                    {
                        throw new ChunkEncodingException("quasi http headers will exceed " +
                            $"max size (> {runningLengthEstimate} > {maxHeadersSize})");
                    }
                    csv.Add(headerRow);
                }
            }

            // serialize to bytes and check that byte count to encode
            // doesn't exceed limit.
            // NB: byte count to encode excludes length in leading row.
            var encoded = Encoding.UTF8.GetBytes(CsvUtils.Serialize(csv));
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
            var encodedLength = lengthToEncode.ToString().PadLeft(
                LengthOfEncodedHeadersLength);
            for (int i = 0; i < LengthOfEncodedHeadersLength; i++)
            {
                encoded[i] = (byte)encodedLength[i];
            }
            return encoded;
        }

        public IQuasiHttpRequestHeaderPart DecodeRequestHeaders(
            byte[] encodedCsv)
        {
            var csv = CsvUtils.Deserialize(Encoding.UTF8.GetString(encodedCsv));
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
            var instance = new DefaultQuasiHttpRequestHeaderPart
            {

            };
            instance.HttpMethod = specialHeader[0];
            instance.Target = specialHeader[1];
            instance.HttpVersion = specialHeader[2];
            try
            {
                instance.ContentLength = IOUtils.ParseInt48(
                    specialHeader[3]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http request content length");
            }
            instance.Headers = DecodeRemainingHeaders(csv);
            return instance;
        }

        public IQuasiHttpResponseHeaderPart DecodeResponseHeaders(
            byte[] encodedCsv)
        {
            var csv = CsvUtils.Deserialize(Encoding.UTF8.GetString(encodedCsv));
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
            var instance = new DefaultQuasiHttpResponseHeaderPart
            {

            };
            try
            {
                instance.StatusCode = int.Parse(specialHeader[0]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http response status code");
            }
            instance.HttpStatusMessage = specialHeader[1];
            instance.HttpVersion = specialHeader[2];
            try
            {
                instance.ContentLength = IOUtils.ParseInt48(
                    specialHeader[3]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http response content length");
            }
            instance.Headers = DecodeRemainingHeaders(csv);
            return instance;
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

        public List<byte[]> GenerateBodyChunksV1(byte[] data)
        {
            return GenerateBodyChunksV1(data, 0, data.Length);
        }

        public List<byte[]> GenerateBodyChunksV1(
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

        private int GenerateBodyChunksV1Internal(byte[] data,
            int offset, int length, List<byte[]> chunks)
        {
            int bytesToRead = Math.Min(length, MaxBodyChunkLength);
            var chunk = new byte[bytesToRead + LengthOfEncodedBodyChunkLength];
            var encodedLength = bytesToRead.ToString().PadLeft(
                LengthOfEncodedBodyChunkLength, '0');
            for (int i = 0; i < LengthOfEncodedBodyChunkLength; i++)
            {
                chunk[i] = (byte)encodedLength[i];
            }
            Array.Copy(data, offset, chunk, LengthOfEncodedBodyChunkLength,
                bytesToRead);
            chunks.Add(chunk);
            return bytesToRead;
        }

        public byte[] GenerateTerminatingBodyChunkV1()
        {
            return Encoding.ASCII.GetBytes(
                "".PadRight(LengthOfEncodedBodyChunkLength, '0'));
        }

        public async Task<(int, bool)> EncodeBodyChunkV1(object reader,
            byte[] data, int offset, int length)
        {
            int totalBytesRead = 0;
            bool firstTime = true;
            do
            {
                var (bytesRead, lastChunkSeen) = await EncodeBodyChunkV1Internal(
                    reader, firstTime,
                    data,
                    offset + totalBytesRead, length - totalBytesRead);
                totalBytesRead += bytesRead;
                firstTime = false;
                if (lastChunkSeen)
                {
                    return (totalBytesRead, true);
                }
                if (bytesRead == 0)
                {
                    break;
                }
            } while (totalBytesRead < length);
            return (totalBytesRead, false);
        }

        private async Task<(int, bool)> EncodeBodyChunkV1Internal(
            object reader, bool firstTime,
            byte[] data, int offset, int length)
        {
            int bytesToRead = Math.Min(
                length - LengthOfEncodedBodyChunkLength,
                MaxBodyChunkLength);
            // allow first time zero byte reads to touch reader.
            if (bytesToRead == 0 && !firstTime)
            {
                return (0, false);
            }
            if (bytesToRead < 0)
            {
                if (firstTime)
                {
                    throw new ArgumentException("length is too small to be valid " +
                        "quasi http body chunk: " + length);
                }
                return (0, false);
            }
            int bytesRead = await IOUtils.ReadBytes(reader, data,
                offset + LengthOfEncodedBodyChunkLength,
                bytesToRead);
            if (bytesRead == 0)
            {
                // any time zero-length chunk is seen, return immediately to
                // avoid unintentional writing of terminating chunk.
                if (bytesToRead == 0)
                {
                    return (0, false);
                }
            }
            var encodedLength = bytesRead.ToString().PadLeft(
                LengthOfEncodedBodyChunkLength, '0');
            for (int i = 0; i < LengthOfEncodedBodyChunkLength; i++)
            {
                data[offset + i] = (byte)encodedLength[i];
            }
            return (bytesRead + LengthOfEncodedBodyChunkLength, bytesRead == 0);
        }

        public async Task<int> DecodeBodyChunkV1Header(object reader)
        {
            await IOUtils.ReadBytesFully(reader,
                DecodingBuffer, 0, DecodingBuffer.Length);
            var csv = CsvUtils.Deserialize(Encoding.UTF8.GetString(
                DecodingBuffer));
            if (csv.Count == 0)
            {
                throw new ArgumentException("invalid quasi http body chunk header");
            }
            var specialHeader = csv[0];
            if (specialHeader.Count < 1)
            {
                throw new ArgumentException("invalid quasi http body chunk header");
            }
            var lengthOfData = int.Parse(specialHeader[0]);
            if (lengthOfData < 0)
            {
                throw new ArgumentException("invalid quasi http body chunk length " +
                    $"({lengthOfData})");
            }
            return lengthOfData;
        }
    }
}