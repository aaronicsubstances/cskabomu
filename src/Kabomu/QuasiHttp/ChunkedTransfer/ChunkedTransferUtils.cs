﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    public static class ChunkedTransferUtils
    {
        /// <summary>
        /// The default value of max chunk size used by quasi http servers and clients. Equal to 8,192 bytes.
        /// </summary>
        public static readonly int DefaultMaxChunkSize = IOUtils.DefaultReadBufferSize;

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
        /// Constant used internally to indicate the number of bytes used to encode the length
        /// of a lead or subsequent chunk, which is 3.
        /// </summary>
        internal static readonly int LengthOfEncodedChunkLength = 3;

        /// <summary>
        /// Constant which communicates the largest chunk size possible with the standard chunk transfer 
        /// implementation in the Kabomu library, and that is currently the largest
        /// signed integer that can fit into 3 bytes.
        /// </summary>
        public static readonly int HardMaxChunkSizeLimit = 1 << 8 * LengthOfEncodedChunkLength - 1 - 1;

        internal static readonly int ReservedBytesToUse;

        private static readonly ByteBufferSlice[] ChunkPrefix;
        private static readonly int ChunkPrefixLength;

        static ChunkedTransferUtils()
        {
            ChunkPrefix = new SubsequentChunk
            {
                Version = LeadChunk.Version01
            }.Serialize();
            ChunkPrefixLength = ChunkPrefix.Sum(s => s.Length);
            ReservedBytesToUse = LengthOfEncodedChunkLength + ChunkPrefixLength;
        }

        internal static void EncodeSubsequentChunkHeader(
            int chunkDataLength, byte[] data, int offset)
        {
            ByteUtils.SerializeUpToInt64BigEndian(
                chunkDataLength + ChunkPrefixLength, data, offset,
                LengthOfEncodedChunkLength);
            int sliceBytesWritten = 0;
            foreach (var slice in ChunkPrefix)
            {
                Array.Copy(slice.Data, slice.Offset,
                    data, offset + LengthOfEncodedChunkLength + sliceBytesWritten,
                    slice.Length);
                sliceBytesWritten += slice.Length;
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
        /// imposes a maximum only on lead chunks exceeding 64KB in size.</param>
        /// <returns>task whose result is a decoded lead chunk.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="reader"/> argument is null</exception>
        /// <exception cref="ChunkDecodingException">If data from reader could not be decoded
        /// into a valid lead chunk.</exception>
        public static async Task<LeadChunk> ReadLeadChunk(ICustomReader reader,
            int maxChunkSize)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            byte[] encodedLength = new byte[LengthOfEncodedChunkLength];
            try
            {
                await IOUtils.ReadBytesFully(reader,
                    encodedLength, 0, encodedLength.Length);
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Failed to decode quasi http headers while " +
                    "reading a chunk length specification", e);
            }

            int chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                 encodedLength.Length, true);
            ValidateChunkLength(chunkLen, maxChunkSize, "Failed to decode quasi http headers");
            var chunkBytes = new byte[chunkLen];
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
                var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
                return chunk;
            }
            catch (Exception e)
            {
                throw new ChunkDecodingException("Encountered invalid chunk of quasi http headers", e);
            }
        }

        internal static void ValidateChunkLength(int chunkLen, int maxChunkSize, string prefix)
        {
            if (chunkLen < 0)
            {
                throw new ChunkDecodingException(
                    $"{prefix}: received negative chunk size of {chunkLen}");
            }
            if (chunkLen > DefaultMaxChunkSizeLimit && chunkLen > maxChunkSize)
            {
                throw new ChunkDecodingException(
                    $"{prefix}: received chunk size of {chunkLen} exceeds" +
                    $" default limit on max chunk size ({DefaultMaxChunkSizeLimit})" +
                    $" as well as maximum configured chunk size of {maxChunkSize}");
            }
        }

        /// <summary>
        /// Helper function for writing out quasi http headers. Quasi http headers are encoded
        /// as the leading chunk before any subsequent chunk representing part of the data of an http body.
        /// </summary>
        /// <param name="writer">the destination to write to</param>
        /// <param name="chunk">the lead chunk containing http headers to be written</param>
        /// <param name="maxChunkSize">the maximum size of the lead chunk.</param>
        /// <returns>a task representing the asynchronous write operation.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="writer"/> or
        /// the <paramref name="chunk"/> argument is null.</exception>
        /// <exception cref="ArgumentException">The size of the data in <paramref name="chunk"/> argument 
        /// is larger than the <paramref name="maxChunkSize"/> argument, or is larger than value of
        /// <see cref="HardMaxChunkSizeLimit"/> field.</exception>
        public static async Task WriteLeadChunk(ICustomWriter writer,
            int maxChunkSize, LeadChunk chunk)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var slices = chunk.Serialize();
            int byteCount = slices.Sum(s => s.Length);
            if (byteCount > maxChunkSize)
            {
                throw new ArgumentException($"headers larger than max chunk size of {maxChunkSize}");
            }
            if (byteCount > HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"headers larger than max chunk size limit of {HardMaxChunkSizeLimit}");
            }
            var encodedLength = new byte[LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await writer.WriteBytes(encodedLength, 0, encodedLength.Length);
            foreach (var slice in slices)
            {
                await writer.WriteBytes(slice.Data, slice.Offset, slice.Length);
            }
        }
    }
}