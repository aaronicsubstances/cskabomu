using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// Structure used to encode quasi http headers for serialization and transmission on
    /// quasi http transports. All properties in this structure are optional except for Version.
    /// </summary>
    /// <remarks>
    /// This structure is equivalent to the information contained in
    /// HTTP request line, HTTP status line, and HTTP request and response headers.
    /// </remarks>
    public class LeadChunk
    {
        /// <summary>
        /// Current version of standard chunk serialization format.
        /// </summary>
        public const byte Version01 = 1;

        /// <summary>
        /// Gets or sets the serialization format version.
        /// </summary>
        public byte Version { get; set; }

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        public byte Flags { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of request target component of HTTP request line.
        /// </summary>
        public string RequestTarget { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP response status code.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets or sets a value providing the length in bytes of a quasi http body which will
        /// follow the lead chunk when serialized. Equivalent to Content-Type and 
        /// Transfer-Encoding=chunked HTTP headers.
        /// </summary>
        /// <remarks>
        /// There are three possible values:
        /// <list type="number">
        /// <item>zero: this means that there will be no quasi http body.</item>
        /// <item>positive: this means that there will be a quasi http body with the exact number of bytes
        /// present as the value of this property.</item>
        /// <item>negative: this means that there will be a quasi http body, but with an unknown number of
        /// bytes.
        /// <para></para>
        /// This implies chunk encoding where one or more subsequent chunks will follow the
        /// lead chunk when serialized.
        /// </item>
        /// </list>
        /// </remarks>
        public long ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of the Content-Type header of HTTP.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of method component of HTTP request line.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets an HTTP request or response version value.
        /// </summary>
        public string HttpVersion { get; set; }

        /// <summary>
        /// Gets or sets HTTP status text, ie the reason phrase component of HTTP response lines.
        /// </summary>
        public string HttpStatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the equivalent of HTTP request or response headers. Null keys and values are not allowed.
        /// </summary>
        /// <remarks>
        /// Unlike in HTTP, here the headers are distinct from properties of this structure equivalent to 
        /// HTTP headers, such as Content-Length and Content-Type. So setting a Content-Length header
        /// here will have no bearing on how to transmit or receive quasi http bodies.
        /// </remarks>
        public IDictionary<string, IList<string>> Headers { get; set; }

        /// <summary>
        /// Serializes the structure into bytes. The serialization format version must be set, or
        /// else deserialization will fail later on. Also headers without values will be skipped.
        /// </summary>
        /// <returns>serialized chunk as a list of byte buffer slices.</returns>
        public ByteBufferSlice[] Serialize()
        {
            var serialized = new ByteBufferSlice[2];

            var csvDataPrefix = new byte[] { Version, Flags };
            serialized[0] = new ByteBufferSlice
            {
                Data = csvDataPrefix,
                Length = csvDataPrefix.Length
            };

            var csvData = new List<IList<string>>();
            var specialHeaderRow = new List<string>();
            specialHeaderRow.Add((RequestTarget != null ? 1 : 0).ToString());
            specialHeaderRow.Add(RequestTarget ?? "");
            specialHeaderRow.Add(StatusCode.ToString());
            specialHeaderRow.Add(ContentLength.ToString());
            specialHeaderRow.Add((ContentType != null ? 1 : 0).ToString());
            specialHeaderRow.Add(ContentType ?? "");
            specialHeaderRow.Add((Method != null ? 1 : 0).ToString());
            specialHeaderRow.Add(Method ?? "");
            specialHeaderRow.Add((HttpVersion != null ? 1 : 0).ToString());
            specialHeaderRow.Add(HttpVersion ?? "");
            specialHeaderRow.Add((HttpStatusMessage != null ? 1 : 0).ToString());
            specialHeaderRow.Add(HttpStatusMessage ?? "");
            csvData.Add(specialHeaderRow);
            if (Headers != null)
            {
                foreach (var header in Headers)
                {
                    if (header.Value.Count == 0)
                    {
                        continue;
                    }
                    var headerRow = new List<string> { header.Key };
                    headerRow.AddRange(header.Value);
                    csvData.Add(headerRow);
                }
            }
            var csv = CsvUtils.Serialize(csvData);
            var csvBytes = ByteUtils.StringToBytes(csv);
            serialized[1] = new ByteBufferSlice
            {
                Data = csvBytes,
                Length = csvBytes.Length
            };
            return serialized;
        }

        /// <summary>
        /// Deserializes the structure from byte buffer. The serialization format version must be present.
        /// Also headers without values will be skipped.
        /// </summary>
        /// <param name="data">the source byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to use</param>
        /// <returns>deserialized lead chunk structure</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/> arguments 
        /// genereate invalid positions in source byte buffer.</exception>
        /// <exception cref="Exception">The byte buffer slice provided does not represent valid
        /// lead chunk structure, or serialization format version is zero, or deserialization failed.</exception>
        public static LeadChunk Deserialize(byte[] data, int offset, int length)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid payload");
            }

            if (length < 10)
            {
                throw new ArgumentException("too small to be a valid lead chunk");
            }

            var instance = new LeadChunk();
            instance.Version = data[offset];
            if (instance.Version == 0)
            {
                throw new ArgumentException("version not set");
            }
            instance.Flags = data[offset + 1];

            var csv = ByteUtils.BytesToString(data, offset + 2, length - 2);
            var csvData = CsvUtils.Deserialize(csv);
            if (csvData.Count == 0)
            {
                throw new ArgumentException("invalid lead chunk");
            }
            var specialHeader = csvData[0];
            if (specialHeader.Count < 12)
            {
                throw new ArgumentException("invalid special header");
            }
            if (specialHeader[0] != "0")
            {
                instance.RequestTarget = specialHeader[1];
            }
            instance.StatusCode = int.Parse(specialHeader[2]);
            instance.ContentLength = long.Parse(specialHeader[3]);
            if (specialHeader[4] != "0")
            {
                instance.ContentType = specialHeader[5];
            }
            if (specialHeader[6] != "0")
            {
                instance.Method = specialHeader[7];
            }
            if (specialHeader[8] != "0")
            {
                instance.HttpVersion = specialHeader[9];
            }
            if (specialHeader[10] != "0")
            {
                instance.HttpStatusMessage = specialHeader[11];
            }
            for (int i = 1; i < csvData.Count; i++)
            {
                var headerRow = csvData[i];
                if (headerRow.Count < 2)
                {
                    continue;
                }
                var headerValue = headerRow.Skip(1).ToList();
                if (instance.Headers == null)
                {
                    instance.Headers = new Dictionary<string, IList<string>>();
                }
                instance.Headers.Add(headerRow[0], headerValue);
            }

            return instance;
        }
    }
}
