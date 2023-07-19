using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private byte[] _csvDataPrefix;
        private IList<IList<string>> _csvData;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public LeadChunk()
        {

        }


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
        /// Serializes the structure into an internal representation. The serialization format version must be set, or
        /// else deserialization will fail later on. Also headers without values will be skipped.
        /// </summary>
        public void UpdateSerializedRepresentation()
        {
            _csvDataPrefix = new byte[] { Version, Flags };

            _csvData = new List<IList<string>>();
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
            _csvData.Add(specialHeaderRow);
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
                    _csvData.Add(headerRow);
                }
            }
        }

        /// <summary>
        /// Gets the size of the serialized representation saved internally
        /// by calling <see cref="UpdateSerializedRepresentation"/>.
        /// </summary>
        /// <returns>size of serialized representation</returns>
        /// <exception cref="InvalidOperationException">If <see cref="UpdateSerializedRepresentation"/>
        /// has not been called</exception>
        public int CalculateSizeInBytesOfSerializedRepresentation()
        {
            if (_csvDataPrefix == null || _csvData == null)
            {
                throw new InvalidOperationException("missing serialized representation");
            }
            int desiredSize = _csvDataPrefix.Length;
            foreach (var row in _csvData)
            {
                var addCommaSeparator = false;
                foreach (var value in row)
                {
                    if (addCommaSeparator)
                    {
                        desiredSize++;
                    }
                    desiredSize += CalculateSizeInBytesOfEscapedValue(value);
                    addCommaSeparator = true;
                }
                desiredSize++; // for newline
            }
            return desiredSize;
        }

        private static int CalculateSizeInBytesOfEscapedValue(string raw)
        {
            var valueContainsSpecialCharacters = false;
            int doubleQuoteCount = 0;
            foreach (var c in raw)
            {
                if (c == ',' || c == '"' || c == '\r' || c == '\n')
                {
                    valueContainsSpecialCharacters = true;
                    if (c == '"')
                    {
                        doubleQuoteCount++;
                    }
                }
            }
            // escape empty strings with two double quotes to resolve ambiguity
            // between an empty row and a row containing an empty string - otherwise both
            // serialize to the same CSV output.
            int desiredSize = Encoding.UTF8.GetByteCount(raw);
            if (raw == "" || valueContainsSpecialCharacters)
            {
                desiredSize += doubleQuoteCount + 2; // for quoting and surrounding double quotes.
            }
            return desiredSize;
        }

        /// <summary>
        /// Writes out the serialized representation generated internally by 
        /// calling <see cref="UpdateSerializedRepresentation"/> as bytes.
        /// </summary>
        /// <param name="writer">The destination of the bytes to be written</returns>
        /// <exception cref="InvalidOperationException">If <see cref="UpdateSerializedRepresentation"/>
        /// has not been called</exception>
        public async Task WriteOutSerializedRepresentation(ICustomWriter writer)
        {
            if (_csvDataPrefix == null || _csvData == null)
            {
                throw new InvalidOperationException("missing serialized representation");
            }
            await writer.WriteBytes(_csvDataPrefix, 0, _csvDataPrefix.Length);
            await CsvUtils.SerializeTo(_csvData, writer);
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
