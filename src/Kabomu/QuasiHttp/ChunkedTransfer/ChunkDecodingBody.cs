﻿using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk decoder of byte streams in the Kabomu library. Wraps a quasi http body and assumes it consists of
    /// an unknown number of one or more <see cref="SubsequentChunk"/> instances ordered consecutively, in which the last
    /// instances has zero data length and all the previous ones have non-empty data.
    /// </summary>
    public class ChunkDecodingBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private readonly IQuasiHttpBody _wrappedBody;
        private readonly int _maxChunkSize;
        private SubsequentChunk _lastChunk;
        private int _lastChunkUsedBytes;

        /// <summary>
        /// Constructor for decoding chunks previously encoded in the data of another quasi http body instance.
        /// </summary>
        /// <param name="wrappedBody">the quasi http body instance to decode.</param>
        /// <param name="maxChunkSize">the maximum allowable size of a chunk seen in the body instance being decoded.
        /// NB: values less than 64KB are always accepted, and so this parameter imposes a maximum only on chunks
        /// with lengths greater than 64KB.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="wrappedBody"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="maxChunkSize"/> argument is zero or negative.</exception>
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

        /// <summary>
        /// Returns -1 to indicate unknown length.
        /// </summary>
        public long ContentLength => -1;

        /// <summary>
        /// Same as the content type of the body instance being decoded (ie the one provided at construction time).
        /// </summary>
        public string ContentType => _wrappedBody.ContentType;

        /// <summary>
        /// Helper function for reading quasi http headers from transports. Quasi http headers are encoded
        /// as the leading chunk before any subsequent chunk representing part of the data of an http body.
        /// Hence quasi http headers are decoded in the same way as http body data chunks.
        /// </summary>
        /// <param name="transport">the quasi http transport to read from</param>
        /// <param name="connection">the particular connection of the transport to read from</param>
        /// <param name="maxChunkSize">the maximum allowable size of the lead chunk to be decoded; effectively this
        /// determines the maximum combined size of quasi http headers to be decoded. NB: This parameter
        /// imposes a maximum only on lead chunks exceeding 64KB in size.</param>
        /// <returns>task whose result is a decoded lead chunk.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="transport"/> argument is null</exception>
        /// <exception cref="ChunkDecodingException">If data on transport's connection could not be decoded
        /// into a valid lead chunk.</exception>
        public static async Task<LeadChunk> ReadLeadChunk(IQuasiHttpTransport transport, object connection,
            int maxChunkSize)
        {
            if (transport == null)
            {
                throw new ArgumentNullException("null transport");
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
                    "reading a chunk length specification", e);
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
                    "reading in chunk data", e);
            }

            try
            {
                var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
                return chunk;
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Encountered invalid chunk of quasi http headers", e);
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
                    "reading a chunk length specification", e);
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
                    "reading in chunk data", e);
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            try
            {
                _lastChunk = SubsequentChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Encountered invalid chunked quasi http body", e);
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
            if (!_readCancellationHandle.Cancel())
            {
                return;
            }
            await _wrappedBody.EndRead();
        }
    }
}