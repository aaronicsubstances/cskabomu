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
        /// The default value of max chunk size used by quasi http servers and clients.
        /// Equal to 8,192 bytes.
        /// </summary>
        public static readonly int DefaultMaxChunkSize = 8_192;

        /// <summary>
        /// The number of ASCII bytes which indicate the length of
        /// the entire request/response header part.
        /// </summary>
        public const int EncodedHeadersLength = 6;
        public const int MaxHeadersLength = 999_999;

        public const int EncodedBodyChunkLength = 10;
        public const int MaxBodyChunkLength = 1_000_000_000;

        private readonly byte[] DecodingBuffer = new byte[EncodedBodyChunkLength];

        public byte[] EncodeRequestHeaders(
            IQuasiHttpRequestHeaderPart reqHeaders, int? maxHeadersSize)
        {
            int maxChunkSize = maxHeadersSize ?? 0;
            if (maxChunkSize <= 0)
            {
                maxChunkSize = DefaultMaxChunkSize;
            }
            var csv = new List<IList<string>>();
            var leadingRow = new List<string>
            {
                "".PadLeft(EncodedHeadersLength, '0'),
                reqHeaders.HttpMethod ?? "",
                reqHeaders.Target ?? "",
                reqHeaders.HttpVersion ?? "",
                reqHeaders.ContentLength.ToString()
            };
            int runningLengthEstimate = leadingRow.Sum(x => x.Length);
            if (runningLengthEstimate > maxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http request headers will exceed " +
                    $"max chunk size (> {runningLengthEstimate} > {maxChunkSize})");
            }
            csv.Add(leadingRow);
            var extraHeaders = reqHeaders.Headers;
            if (extraHeaders != null)
            {
                foreach (var header in extraHeaders)
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
                        throw new ChunkEncodingException("quasi http request headers will exceed " +
                            $"max chunk size (> {runningLengthEstimate} > {maxChunkSize})");
                    }
                    csv.Add(headerRow);
                }
            }

            // add terminating header row.
            csv.Add(new List<string>());

            // finally serialize to bytes and check that byte count
            // doesn't exceed limit.
            var encoded = Encoding.UTF8.GetBytes(CsvUtils.Serialize(csv));
            if (encoded.Length > maxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http request headers exceed " +
                    $"max chunk size ({encoded.Length} > {maxChunkSize})");
            }
            if (encoded.Length > MaxHeadersLength)
            {
                throw new ChunkEncodingException("quasi http request headers too " +
                    $"large ({encoded.Length} > {MaxHeadersLength})");
            }
            var encodedLength = encoded.Length.ToString().PadLeft('0');
            for (int i = 0; i < EncodedHeadersLength; i++)
            {
                encoded[i] = (byte)encodedLength[i];
            }
            return encoded;
        }

        public byte[] EncodeResponseHeaders(
            IQuasiHttpResponseHeaderPart resHeaders, int? maxHeadersSize)
        {
            int maxChunkSize = maxHeadersSize ?? 0;
            if (maxChunkSize <= 0)
            {
                maxChunkSize = DefaultMaxChunkSize;
            }
            var csv = new List<IList<string>>();
            var leadingRow = new List<string>
            {
                "".PadLeft(EncodedHeadersLength, '0'),
                resHeaders.StatusCode.ToString(),
                resHeaders.HttpStatusMessage ?? "",
                resHeaders.HttpVersion ?? "",
                resHeaders.ContentLength.ToString()
            };
            int runningLengthEstimate = leadingRow.Sum(x => x.Length);
            if (runningLengthEstimate > maxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http response headers will exceed " +
                    $"max chunk size (> {runningLengthEstimate} > {maxChunkSize})");
            }
            csv.Add(leadingRow);
            var extraHeaders = resHeaders.Headers;
            if (extraHeaders != null)
            {
                foreach (var header in extraHeaders)
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
                        throw new ChunkEncodingException("quasi http response headers will exceed " +
                            $"max chunk size (> {runningLengthEstimate} > {maxChunkSize})");
                    }
                    csv.Add(headerRow);
                }
            }

            // add terminating header row.
            csv.Add(new List<string>());

            // finally serialize to bytes and check that byte count
            // doesn't exceed limit.
            var encoded = Encoding.UTF8.GetBytes(CsvUtils.Serialize(csv));
            if (encoded.Length > maxHeadersSize)
            {
                throw new ChunkEncodingException("quasi http response headers exceed " +
                    $"max chunk size ({encoded.Length} > {maxChunkSize})");
            }
            if (encoded.Length > MaxHeadersLength)
            {
                throw new ChunkEncodingException("quasi http response headers too " +
                    $"large ({encoded.Length} > {MaxHeadersLength})");
            }
            var encodedLength = encoded.Length.ToString().PadLeft('0');
            for (int i = 0; i < EncodedHeadersLength; i++)
            {
                encoded[i] = (byte)encodedLength[i];
            }
            return encoded;
        }

        public IQuasiHttpRequestHeaderPart DecodeRequestHeaders(
            byte[] encodedCsv)
        {
            var csv = CsvUtils.Deserialize(Encoding.UTF8.GetString(encodedCsv));
            if (csv.Count == 0)
            {
                throw new ArgumentException("invalid encoded quasi http request headers");
            }
            var specialHeader = csv[0];
            if (specialHeader.Count < 5)
            {
                throw new ArgumentException("invalid quasi http request line");
            }
            var instance = new DefaultQuasiHttpRequestHeaderPart
            {

            };
            // skip length prefix column
            instance.HttpMethod = specialHeader[1];
            instance.Target = specialHeader[2];
            instance.HttpVersion = specialHeader[3];
            try
            {
                instance.ContentLength = IOUtils.ParseInt48(specialHeader[4]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http request content length");
            }
            for (int i = 1; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                if (instance.Headers == null)
                {
                    instance.Headers = new Dictionary<string, IList<string>>();
                }
                string headerName = headerRow[0];
                if (!instance.Headers.ContainsKey(headerName))
                {
                    instance.Headers.Add(headerName, new List<string>());
                }
                var headerValues = instance.Headers[headerName];
                foreach (var headerValue in headerRow.Skip(1))
                {
                    headerValues.Add(headerValue);
                }
            }

            return instance;
        }

        public IQuasiHttpResponseHeaderPart DecodeResponseHeaders(
            byte[] encodedCsv)
        {
            var csv = CsvUtils.Deserialize(Encoding.UTF8.GetString(encodedCsv));
            if (csv.Count == 0)
            {
                throw new ArgumentException("invalid encoded quasi http response headers");
            }
            var specialHeader = csv[0];
            if (specialHeader.Count < 5)
            {
                throw new ArgumentException("invalid quasi http response status line");
            }
            var instance = new DefaultQuasiHttpResponseHeaderPart
            {

            };
            // skip length prefix column
            try
            {
                instance.StatusCode = int.Parse(specialHeader[1]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http response status code");
            }
            instance.HttpStatusMessage = specialHeader[2];
            instance.HttpVersion = specialHeader[3];
            try
            {
                instance.ContentLength = IOUtils.ParseInt48(specialHeader[4]);
            }
            catch
            {
                throw new ArgumentException("invalid quasi http response content length");
            }
            for (int i = 1; i < csv.Count; i++)
            {
                var headerRow = csv[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                if (instance.Headers == null)
                {
                    instance.Headers = new Dictionary<string, IList<string>>();
                }
                string headerName = headerRow[0];
                if (!instance.Headers.ContainsKey(headerName))
                {
                    instance.Headers.Add(headerName, new List<string>());
                }
                var headerValues = instance.Headers[headerName];
                foreach (var headerValue in headerRow.Skip(1))
                {
                    headerValues.Add(headerValue);
                }
            }

            return instance;
        }

        public async Task<(int, bool)> EncodeSubsequentChunkV1(object reader,
            byte[] data, int offset, int length)
        {
            int totalBytesRead = 0;
            bool firstTime = true;
            do
            {
                var (bytesRead, lastChunkSeen) = await EncodeSubsequentChunkV1Internal(
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
            } while (totalBytesRead < length) ;
                return (totalBytesRead, false);
        }

        private async Task<(int, bool)> EncodeSubsequentChunkV1Internal(
            object reader, bool firstTime,
            byte[] data, int offset, int length)
        {
            int bytesToRead = Math.Min(
                length - EncodedBodyChunkLength,
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
                    throw new ChunkEncodingException("length is too small to be valid " +
                        "subsequent chunk: " + length);
                }
                return (0, false);
            }
            int bytesRead = await IOUtils.ReadBytes(reader, data,
                offset + EncodedBodyChunkLength,
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
            var encodedLength = bytesRead.ToString().PadLeft('0');
            for (int i = 0; i < EncodedBodyChunkLength; i++)
            {
                data[offset + i] = (byte)encodedLength[i];
            }
            return (bytesRead + EncodedBodyChunkLength, bytesRead == 0);
        }

        public async Task<int> DecodeSubsequentChunkV1Header(object reader)
        {
            try
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
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "decoding a chunk header", e);
            }
        }
    }
}