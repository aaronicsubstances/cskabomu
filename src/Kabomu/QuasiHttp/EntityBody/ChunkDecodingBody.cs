using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ChunkDecodingBody : IQuasiHttpBody
    {
        private readonly CancellationTokenSource _readCancellationHandle = new CancellationTokenSource();
        private readonly IQuasiHttpBody _wrappedBody;
        private readonly int _maxChunkSize;
        private SubsequentChunk _lastChunk;
        private int _lastChunkUsedBytes;

        public ChunkDecodingBody(IQuasiHttpBody wrappedBody, int maxChunkSize)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentNullException(nameof(wrappedBody));
            }
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            _wrappedBody = wrappedBody;
            _maxChunkSize = maxChunkSize;
        }

        public long ContentLength => -1;
        public string ContentType => _wrappedBody.ContentType;

        public static async Task<LeadChunk> ReadLeadChunk(IQuasiHttpTransport transport, object connection,
            int maxChunkSize)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            byte[] encodedLength = new byte[ChunkEncodingBody.LengthOfEncodedChunkLength];
            try
            {
                await TransportUtils.ReadTransportBytesFully(transport, connection,
                    encodedLength, 0, encodedLength.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http headers while " +
                    "reading a chunk length specification: " + e.Message, e);
            }

           int chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                encodedLength.Length);
            ValidateChunkLength(chunkLen, maxChunkSize, "Failed to decode quasi http headers");
            var chunkBytes = new byte[chunkLen];
            try
            {
                await TransportUtils.ReadTransportBytesFully(transport, connection,
                    chunkBytes, 0, chunkBytes.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http headers while " +
                    "reading in chunk data: " + e.Message, e);
            }

            try
            {
                var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
                return chunk;
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Encountered invalid chunk of quasi http headers: " + e.Message, e);
            }
        }

        private static void ValidateChunkLength(int chunkLen, int maxChunkSize, string prefix)
        {
            if (chunkLen > TransportUtils.DefaultMaxChunkSizeLimit && chunkLen > maxChunkSize)
            {
                throw new ChunkDecodingException(
                    $"{prefix}: received chunk size of {chunkLen} exceeds" +
                    $" default limit on max chunk size ({TransportUtils.DefaultMaxChunkSizeLimit})" +
                    $" as well as maximum configured chunk size of {maxChunkSize}");
            }
        }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            var encodedLength = new byte[ChunkEncodingBody.LengthOfEncodedChunkLength];
            // once empty data chunk is seen, return 0 for all subsequent reads.
            if (_lastChunk != null && (_lastChunk.DataLength == 0 || _lastChunkUsedBytes < _lastChunk.DataLength))
            {
                return SupplyFromLastChunk(data, offset, bytesToRead);
            }            

            try
            {
                await TransportUtils.ReadBodyBytesFully(_wrappedBody,
                   encodedLength, 0, encodedLength.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "reading a chunk length specification: " + e.Message, e);
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            var chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                encodedLength.Length);
            ValidateChunkLength(chunkLen, _maxChunkSize, "Failed to decode quasi http body");
            var chunkBytes = new byte[chunkLen];

            try
            {
                await TransportUtils.ReadBodyBytesFully(_wrappedBody,
                    chunkBytes, 0, chunkBytes.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http body while " +
                    "reading in chunk data: " + e.Message, e);
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            try
            {
                _lastChunk = SubsequentChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Encountered invalid chunked quasi http body: " + e.Message, e);
            }
            _lastChunkUsedBytes = 0;
            return SupplyFromLastChunk(data, offset, bytesToRead);
        }

        private int SupplyFromLastChunk(byte[] data, int offset, int bytesToRead)
        {
            int lengthToUse = Math.Min(_lastChunk.DataLength - _lastChunkUsedBytes, bytesToRead);
            Array.Copy(_lastChunk.Data, _lastChunk.DataOffset + _lastChunkUsedBytes, data, offset, lengthToUse);
            _lastChunkUsedBytes += lengthToUse;
            return lengthToUse;
        }

        public async Task EndRead()
        {
            _readCancellationHandle.Cancel();
            // take advantage of the fact once wrapped body is not null,
            // no code in this class sets it back to null.
            if (_wrappedBody != null)
            {
                await _wrappedBody.EndRead();
            }
        }
    }
}
