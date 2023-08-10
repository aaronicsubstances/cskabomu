﻿using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// Contains helper functions for implementing the custom chunked transfer
    /// protocol used by the Kabomu libary.
    /// </summary>
    public static class ChunkedTransferUtils
    {
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
        /// Constant used internally to indicate the number of bytes used to encode the length
        /// of a lead or subsequent chunk, which is 3.
        /// </summary>
        internal static readonly int LengthOfEncodedChunkLength = 3;

        /// <summary>
        /// Constant which communicates the largest chunk size possible with the standard chunk transfer 
        /// implementation in the Kabomu library, and that is currently the largest
        /// signed integer that can fit into 3 bytes.
        /// </summary>
        public static readonly int HardMaxChunkSizeLimit = 8_388_607;

        /// <summary>
        /// Encodes a subsequent chunk header to a custom writer.
        /// </summary>
        /// <param name="chunkDataLength">the number of bytes of the
        /// chunk data section which will follow the header.</param>
        /// <param name="writer">destination of encoded subsequent chunk header
        /// acceptable by <see cref="IOUtils.WriteBytes"/> function.</param>
        /// <param name="bufferToUse">optional buffer to use as temporary
        /// storage during encoding. Will be ignored if less than 5 bytes long.</param>
        /// <returns>a task representing asynchronous operation</returns>
        public static Task EncodeSubsequentChunkV1Header(
            int chunkDataLength, object writer, byte[] bufferToUse = null)
        {
            if (bufferToUse == null ||
                bufferToUse.Length < LengthOfEncodedChunkLength + 2)
            {
                bufferToUse = new byte[LengthOfEncodedChunkLength + 2];
            }
            ByteUtils.SerializeUpToInt32BigEndian(
                chunkDataLength + 2, bufferToUse, 0,
                LengthOfEncodedChunkLength);
            bufferToUse[LengthOfEncodedChunkLength] = LeadChunk.Version01;
            bufferToUse[LengthOfEncodedChunkLength + 1] = 0; // flags.
            return IOUtils.WriteBytes(writer, bufferToUse, 0, LengthOfEncodedChunkLength + 2);
        }

        /// <summary>
        /// Decodes a subsequent chunk header from a custom reader.
        /// </summary>
        /// <param name="reader">source of bytes representing subsequent
        /// chunk header which is acceptable by <see cref="IOUtils.ReadBytes"/> function.</param>
        /// <param name="bufferToUse">optional buffer to use as temporary
        /// storage during decoding. Will be ignored if less than 5 bytes long.</param>
        /// <param name="maxChunkSize">the maximum allowable size of the subsequent chunk to be decoded.
        /// NB: This parameter imposes a maximum only on lead chunks exceeding 64KB in size. Can
        /// pass zero to use default value.</param>
        /// <returns>a task whose result will be the number of bytes in the
        /// data following the decoded header.</returns>
        /// <exception cref="ChunkDecodingException">the bytes in the
        /// <see cref="reader"/> argument do not represent a valid subsequent
        /// chunk header in version 1 format.</exception>
        public static async Task<int> DecodeSubsequentChunkV1Header(
            object reader, byte[] bufferToUse = null, int maxChunkSize = 0)
        {
            if (bufferToUse == null ||
                bufferToUse.Length < LengthOfEncodedChunkLength + 2)
            {
                bufferToUse = new byte[LengthOfEncodedChunkLength + 2];
            }
            if (maxChunkSize <= 0)
            {
                maxChunkSize = DefaultMaxChunkSize;
            }
            try
            {
                await IOUtils.ReadBytesFully(reader,
                   bufferToUse, 0, LengthOfEncodedChunkLength + 2);

                var chunkLen = ByteUtils.DeserializeUpToInt32BigEndian(
                    bufferToUse, 0, LengthOfEncodedChunkLength,
                    true);
                ValidateChunkLength(chunkLen, maxChunkSize);

                int version = bufferToUse[LengthOfEncodedChunkLength];
                //int flags = bufferToUse[LengthOfEncodedChunkLength+1];
                if (version != LeadChunk.Version01)
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
        public static async Task<LeadChunk> ReadLeadChunk(object reader,
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
                var chunk = LeadChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
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
        /// is larger than the <paramref name="maxChunkSize"/> argument, or is larger than value of
        /// <see cref="HardMaxChunkSizeLimit"/> field.</exception>
        public static async Task WriteLeadChunk(object writer,
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
            chunk.UpdateSerializedRepresentation();
            int byteCount = chunk.CalculateSizeInBytesOfSerializedRepresentation();
            if (byteCount > maxChunkSize)
            {
                throw new ArgumentException($"headers larger than max chunk size of {maxChunkSize}");
            }
            if (byteCount > HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"headers larger than max chunk size limit of {HardMaxChunkSizeLimit}");
            }
            var encodedLength = new byte[LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt32BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await IOUtils.WriteBytes(writer, encodedLength, 0, encodedLength.Length);
            await chunk.WriteOutSerializedRepresentation(writer);
        }
    }
}
