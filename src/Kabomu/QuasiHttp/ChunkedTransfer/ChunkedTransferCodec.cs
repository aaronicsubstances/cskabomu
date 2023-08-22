using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// Contains helper functions for implementing the custom chunked transfer
    /// protocol used by the Kabomu libary.
    /// </summary>
    public class ChunkedTransferCodec
    {
        /// <summary>
        /// Current version of standard chunk serialization format, which is v1.
        /// </summary>
        public const byte Version01 = 1;

        /// <summary>
        /// The default value of max chunk size used by quasi http servers and clients.
        /// Equal to 8,192 bytes.
        /// </summary>
        public static readonly int DefaultMaxChunkSize = 8_192;

        /// <summary>
        /// The maximum value of a max chunk size that can be tolerated during chunk decoding even if it
        /// exceeds the value used for sending. Equal to 65,536 bytes.
        /// </summary>
        /// <remarks>
        /// Practically this means that communicating parties can safely send chunks not exceeding 64KB without
        /// fear of rejection and without prior negotiation. Beyond 64KB however, communicating parties must have
        /// some prior negotiation (manual or automated) on max chunk sizes, or else chunks may be rejected
        /// by receivers as too large.
        /// </remarks>
        public static readonly int DefaultMaxChunkSizeLimit = 65_536;

        /// <summary>
        /// Constant which communicates the largest chunk size possible with the standard chunk transfer 
        /// implementation in the Kabomu library, and that is currently almost equal to
        /// the largest signed integer that can fit into 3 bytes.
        /// </summary>
        public static readonly int HardMaxChunkSizeLimit = 8_388_500;

        /// <summary>
        /// Constant used internally to indicate the number of bytes used to encode the length
        /// of a lead or subsequent chunk, which is 3.
        /// </summary>
        private static readonly int LengthOfEncodedChunkLength = 3;

        private byte[] _csvDataPrefix;
        private IList<IList<string>> _csvData;
        private readonly byte[] _headerBuffer = new byte[
            LengthOfEncodedChunkLength + 2];

        /// <summary>
        /// Encodes a subsequent chunk header to a custom writer.
        /// </summary>
        /// <param name="chunkDataLength">the number of bytes of the
        /// chunk data section which will follow the header.</param>
        /// <param name="writer">destination of encoded subsequent chunk header
        /// acceptable by <see cref="IOUtils.WriteBytes"/> function.</param>
        /// <returns>a task representing asynchronous operation</returns>
        public Task EncodeSubsequentChunkV1Header(
            int chunkDataLength, object writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            ByteUtils.SerializeUpToInt32BigEndian(
                chunkDataLength + 2, _headerBuffer, 0,
                LengthOfEncodedChunkLength);
            _headerBuffer[LengthOfEncodedChunkLength] = Version01;
            _headerBuffer[LengthOfEncodedChunkLength + 1] = 0; // flags.
            return IOUtils.WriteBytes(writer, _headerBuffer, 0, LengthOfEncodedChunkLength + 2);
        }

        /// <summary>
        /// Decodes a subsequent chunk header from a custom reader.
        /// </summary>
        /// <param name="bufferToUse">optional buffer to use as temporary
        /// storage during decoding. Must be at least 5 bytes long.</param>
        /// <param name="reader">source of bytes representing subsequent
        /// chunk header which is acceptable by <see cref="IOUtils.ReadBytes"/> function.
        /// Must be specified if <paramref name="bufferToUse"/> argument is null.</param>
        /// <param name="maxChunkSize">the maximum allowable size of the subsequent chunk to be decoded.
        /// NB: This parameter imposes a maximum only on lead chunks exceeding 64KB in size. Can
        /// pass zero to use default value.</param>
        /// <returns>a task whose result will be the number of bytes in the
        /// data following the decoded header.</returns>
        /// <exception cref="ChunkDecodingException">the bytes in the
        /// <paramref name="bufferToUse"/> or
        /// <see cref="reader"/> argument do not represent a valid subsequent
        /// chunk header in version 1 format.</exception>
        public async Task<int> DecodeSubsequentChunkV1Header(
            byte[] bufferToUse, object reader, int maxChunkSize = 0)
        {
            if (maxChunkSize <= 0)
            {
                maxChunkSize = DefaultMaxChunkSize;
            }
            if (bufferToUse == null && reader == null)
            {
                throw new ArgumentException("reader arg cannot be null if " +
                    "bufferToUse arg is null");
            }
            try
            {
                if (bufferToUse == null)
                {
                    bufferToUse = _headerBuffer;
                    await IOUtils.ReadBytesFully(reader,
                       bufferToUse, 0, LengthOfEncodedChunkLength + 2);
                }
                var chunkLen = ByteUtils.DeserializeUpToInt32BigEndian(
                    bufferToUse, 0, LengthOfEncodedChunkLength,
                    true);
                ValidateChunkLength(chunkLen, maxChunkSize);

                int version = bufferToUse[LengthOfEncodedChunkLength];
                //int flags = bufferToUse[LengthOfEncodedChunkLength+1];
                if (version != Version01)
                {
                    throw new ArgumentException("version not set to v1");
                }

                int chunkDataLen = chunkLen - 2;
                return chunkDataLen;
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Error encountered while " +
                    "decoding a subsequent chunk header", e);
            }
        }

        /// <summary>
        /// Helper function for reading quasi http headers. Quasi http headers are encoded
        /// as the leading chunk before any subsequent chunk representing part of the data of an http body.
        /// Hence quasi http headers are decoded in the same way as http body data chunks.
        /// </summary>
        /// <param name="reader">the source to read from</param>
        /// <param name="maxChunkSize">the maximum allowable size of the lead chunk to be decoded; effectively this
        /// determines the maximum combined size of quasi http headers to be decoded. NB: This parameter
        /// imposes a maximum only on lead chunks exceeding 64KB in size. Can
        /// pass zero to use default value.</param>
        /// <returns>task whose result is a decoded lead chunk.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="reader"/> argument is null</exception>
        /// <exception cref="ChunkDecodingException">If data from reader could not be decoded
        /// into a valid lead chunk.</exception>
        public async Task<LeadChunk> ReadLeadChunk(object reader,
            int maxChunkSize = 0)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (maxChunkSize <= 0)
            {
                maxChunkSize = DefaultMaxChunkSize;
            }
            byte[] chunkBytes;
            try
            {
                byte[] encodedLength = new byte[LengthOfEncodedChunkLength];
                if (await IOUtils.ReadBytes(reader, encodedLength, 0, 1) <= 0)
                {
                    return null;
                }
                await IOUtils.ReadBytesFully(reader,
                    encodedLength, 1, encodedLength.Length - 1);
                int chunkLen = ByteUtils.DeserializeUpToInt32BigEndian(encodedLength, 0,
                     encodedLength.Length, true);
                ValidateChunkLength(chunkLen, maxChunkSize);
                chunkBytes = new byte[chunkLen];
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http headers while " +
                    "decoding a chunk header", e);
            }

            try
            {
                await IOUtils.ReadBytesFully(reader,
                    chunkBytes, 0, chunkBytes.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http headers while " +
                    "reading in chunk data", e);
            }

            try
            {
                var chunk = Deserialize(chunkBytes, 0, chunkBytes.Length);
                return chunk;
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Encountered invalid chunk of quasi http headers", e);
            }
        }

        private static void ValidateChunkLength(int chunkLen, int maxChunkSize)
        {
            if (chunkLen < 0)
            {
                throw new ArgumentException($"received negative chunk size of {chunkLen}");
            }
            if (chunkLen > DefaultMaxChunkSizeLimit && chunkLen > maxChunkSize)
            {
                throw new ArgumentException(
                    $"received chunk size of {chunkLen} exceeds" +
                    $" default limit on max chunk size ({DefaultMaxChunkSizeLimit})" +
                    $" as well as maximum configured chunk size of {maxChunkSize}");
            }
        }

        /// <summary>
        /// Helper function for writing out quasi http headers. Quasi http headers are encoded
        /// as the leading chunk before any subsequent chunk representing part of the data of an http body.
        /// </summary>
        /// <param name="writer">the destination to write to which is acceptable
        /// by <see cref="IOUtils.WriteBytes"/></param>
        /// <param name="chunk">the lead chunk containing http headers to be written</param>
        /// <param name="maxChunkSize">the maximum size of the lead chunk. Can
        /// pass zero to use default value.</param>
        /// <returns>a task representing the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="writer"/> or
        /// the <paramref name="chunk"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The size of the data in <paramref name="chunk"/> argument 
        /// is larger than the <paramref name="maxChunkSize"/> argument.</exception>
        public async Task WriteLeadChunk(object writer,
            LeadChunk chunk, int maxChunkSize = 0)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            if (maxChunkSize <= 0)
            {
                maxChunkSize = DefaultMaxChunkSize;
            }
            UpdateSerializedRepresentation(chunk);
            int byteCount = CalculateSizeInBytesOfSerializedRepresentation();
            if (byteCount > maxChunkSize)
            {
                throw new ArgumentException($"headers exceed max chunk size of {maxChunkSize}");
            }
            var encodedLength = new byte[LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt32BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await IOUtils.WriteBytes(writer, encodedLength, 0, encodedLength.Length);
            await WriteOutSerializedRepresentation(writer);
        }

        /// <summary>
        /// Updates an instance of <see cref="IQuasiHttpMutableRequest"/>
        /// with corresponding properties on a <see cref="LeadChunk"/> instance,
        /// in particular: method, target, http version and headers.
        /// </summary>
        /// <param name="request">request instance to be updated</param>
        /// <param name="chunk">lead chunk instance which will be used
        /// to update request instance</param>
        public static void UpdateRequest(IQuasiHttpMutableRequest request,
            LeadChunk chunk)
        {
            request.Method = chunk.Method;
            request.Target = chunk.RequestTarget;
            request.Headers = chunk.Headers;
            request.HttpVersion = chunk.HttpVersion;
        }

        /// <summary>
        /// Updates an instance of <see cref="IQuasiHttpMutableResponse"/>
        /// with corresponding properties on a <see cref="LeadChunk"/> instance,
        /// in particular: status code, http status message, http version and headers.
        /// </summary>
        /// <param name="response">response instance to be updated</param>
        /// <param name="chunk">lead chunk instance which will be used
        /// to update response instance</param>
        public static  void UpdateResponse(IQuasiHttpMutableResponse response,
            LeadChunk chunk)
        {
            response.StatusCode = chunk.StatusCode;
            response.HttpStatusMessage = chunk.HttpStatusMessage;
            response.Headers = chunk.Headers;
            response.HttpVersion = chunk.HttpVersion;
        }

        /// <summary>
        /// Creates a new <see cref="LeadChunk"/> instance which is initialized
        /// with the corresponding properties on an instance of the
        /// <see cref="IQuasiHttpRequest"/> interface.
        /// </summary>
        /// <param name="request">request instance which will be used
        /// to initialize newly created lead chunk.</param>
        /// <returns>new instance of lead chunk with version set to v1, and
        /// request-related properties initialized</returns>
        public static LeadChunk CreateFromRequest(IQuasiHttpRequest request)
        {
            var chunk = new LeadChunk
            {
                Version = Version01,
                Method = request.Method,
                RequestTarget = request.Target,
                Headers = request.Headers,
                HttpVersion = request.HttpVersion,
                ContentLength = 0
            };
            var requestBody = request.Body;
            if (requestBody != null)
            {
                chunk.ContentLength = requestBody.ContentLength;
            }
            return chunk;
        }

        /// <summary>
        /// Creates a new <see cref="LeadChunk"/> instance which is initialized
        /// with the corresponding properties on an instance of the
        /// <see cref="IQuasiHttpResponse"/> interface.
        /// </summary>
        /// <param name="response">response instance which will be used
        /// to initialize newly created lead chunk.</param>
        /// <returns>new instance of lead chunk with version set to v1, and
        /// response-related properties initialized</returns>
        public static LeadChunk CreateFromResponse(IQuasiHttpResponse response)
        {
            var chunk = new LeadChunk
            {
                Version = Version01,
                StatusCode = response.StatusCode,
                HttpStatusMessage = response.HttpStatusMessage,
                Headers = response.Headers,
                HttpVersion = response.HttpVersion,
                ContentLength = 0
            };
            var responseBody = response.Body;
            if (responseBody != null)
            {
                chunk.ContentLength = responseBody.ContentLength;
            }
            return chunk;
        }

        /// <summary>
        /// Serializes the structure into an internal representation. The serialization format version must be set, or
        /// else deserialization will fail later on. Also headers without values will be skipped.
        /// </summary>
        internal void UpdateSerializedRepresentation(LeadChunk chunk)
        {
            _csvDataPrefix = new byte[] { chunk.Version, chunk.Flags };

            _csvData = new List<IList<string>>();
            var specialHeaderRow = new List<string>();
            specialHeaderRow.Add((chunk.RequestTarget != null ? 1 : 0).ToString());
            specialHeaderRow.Add(chunk.RequestTarget ?? "");
            specialHeaderRow.Add(chunk.StatusCode.ToString());
            specialHeaderRow.Add(chunk.ContentLength.ToString());
            specialHeaderRow.Add((chunk.Method != null ? 1 : 0).ToString());
            specialHeaderRow.Add(chunk.Method ?? "");
            specialHeaderRow.Add((chunk.HttpVersion != null ? 1 : 0).ToString());
            specialHeaderRow.Add(chunk.HttpVersion ?? "");
            specialHeaderRow.Add((chunk.HttpStatusMessage != null ? 1 : 0).ToString());
            specialHeaderRow.Add(chunk.HttpStatusMessage ?? "");
            _csvData.Add(specialHeaderRow);
            if (chunk.Headers != null)
            {
                foreach (var header in chunk.Headers)
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
        internal int CalculateSizeInBytesOfSerializedRepresentation()
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
        /// Transfers the serialized representation generated internally by 
        /// calling <see cref="UpdateSerializedRepresentation"/> to a custom writer.
        /// </summary>
        /// <param name="writer">The destination of the bytes to be written
        /// which is acceptable by <see cref="IOUtils.WriteBytes"/> function</returns>
        /// <exception cref="InvalidOperationException">If <see cref="UpdateSerializedRepresentation"/>
        /// has not been called</exception>
        internal async Task WriteOutSerializedRepresentation(object writer)
        {
            if (_csvDataPrefix == null || _csvData == null)
            {
                throw new InvalidOperationException("missing serialized representation");
            }
            await IOUtils.WriteBytes(writer, _csvDataPrefix, 0, _csvDataPrefix.Length);
            await CsvUtils.SerializeTo(_csvData, writer);
        }

        /// <summary>
        /// Deserializes the structure from byte buffer. The serialization format version must be present.
        /// Also headers without values will be skipped.
        /// </summary>
        /// <param name="data">the source byte buffer</param>
        /// <param name="offset">the start decoding position in data</param>
        /// <param name="length">the number of bytes to deserialize</param>
        /// <returns>deserialized lead chunk structure</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="data"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/> arguments 
        /// genereate invalid positions in source byte buffer.</exception>
        /// <exception cref="Exception">The byte buffer slice provided does not represent valid
        /// lead chunk structure, or serialization format version is zero, or deserialization failed.</exception>
        internal static LeadChunk Deserialize(byte[] data, int offset, int length)
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
            if (instance.Version != Version01)
            {
                throw new ArgumentException("version not set to v1");
            }
            instance.Flags = data[offset + 1];

            var csv = ByteUtils.BytesToString(data, offset + 2, length - 2);
            var csvData = CsvUtils.Deserialize(csv);
            if (csvData.Count == 0)
            {
                throw new ArgumentException("invalid lead chunk");
            }
            var specialHeader = csvData[0];
            if (specialHeader.Count < 10)
            {
                throw new ArgumentException("invalid special header");
            }
            if (specialHeader[0] == "1")
            {
                instance.RequestTarget = specialHeader[1];
            }
            try
            {
                instance.StatusCode = int.Parse(specialHeader[2]);
            }
            catch
            {
                throw new ArgumentException("invalid status code");
            }
            try
            {
                instance.ContentLength = ByteUtils.ParseInt48(specialHeader[3]);
            }
            catch
            {
                throw new ArgumentException("invalid content length");
            }
            if (specialHeader[4] == "1")
            {
                instance.Method = specialHeader[5];
            }
            if (specialHeader[6] == "1")
            {
                instance.HttpVersion = specialHeader[7];
            }
            if (specialHeader[8] == "1")
            {
                instance.HttpStatusMessage = specialHeader[9];
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
