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
    /// Provides functions for writing and reading of data in byte chunks
    /// formatted int TlV (ie tag-length-value) format.
    /// </summary>
    public static class TlvUtils
    {
        /// <summary>
        /// Tag number for quasi http headers.
        /// </summary>
        public const int TagForQuasiHttpHeaders = 0x68647273;

        /// <summary>
        /// Tag number for quasi http body chunks.
        /// </summary>
        public const int TagForQuasiHttpBodyChunk = 0x62647461;

        /// <summary>
        /// Tag number for quasi http body chunk extensions.
        /// </summary>
        public const int TagForQuasiHttpBodyChunkExt = 0x62657874;

        /// <summary>
        /// Generates an 8-byte buffer consisting of tag and length.
        /// </summary>
        /// <param name="tag">positive number</param>
        /// <param name="length">non negative number</param>
        /// <returns>buffer with tag and length serialized</returns>
        /// <exception cref="ArgumentException">The <paramref name="tag"/>
        /// argument is not positive, or the <paramref name="length"/>> argument
        /// is negative.</exception>
        public static byte[] EncodeTagAndLengthOnly(int tag, int length)
        {
            if (tag <= 0)
            {
                throw new ArgumentException("invalid tag: " + tag);
            }
            if (length < 0)
            {
                throw new ArgumentException("invalid tag value length: " +
                    length);
            }
            var tagAndLen = new byte[8];
            MiscUtilsInternal.SerializeInt32BE(tag, tagAndLen, 0);
            MiscUtilsInternal.SerializeInt32BE(length, tagAndLen, 4);
            return tagAndLen;
        }

        /// <summary>
        /// Writes out an 8-byte buffer consisting of a tag and zero length
        /// </summary>
        /// <param name="stream">destination of write</param>
        /// <param name="tag">positive number to write out</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>a task representing asynchronous operation</returns>
        /// <exception cref="ArgumentException">The
        /// <paramref name="tag"/> argument is not positive.</exception>
        public static async Task WriteEndOfTlvStream(Stream stream,
            int tag,
            CancellationToken cancellationToken = default)
        {
            if (tag <= 0)
            {
                throw new ArgumentException("invalid tag: " + tag);
            }
            await stream.WriteAsync(
                EncodeTagAndLengthOnly(tag, 0),
                cancellationToken);
        }

        /// <summary>
        /// Decodes a 4-byte buffer slice into a positive number
        /// representing a tag.
        /// </summary>
        /// <param name="data">source buffer</param>
        /// <param name="offset">starting position in source buffer</param>
        /// <returns>decoded positive number</returns>
        /// <exception cref="ArgumentException">The decoded tag is
        /// not positive.</exception>
        public static int DecodeTag(byte[] data, int offset)
        {
            int tag = MiscUtilsInternal.DeserializeInt32BE(
                data, offset);
            if (tag <= 0)
            {
                throw new ArgumentException("invalid tag: " +
                    tag);
            }
            return tag;
        }

        private static int DecodeTagObtainedFromStream(byte[] data, int offset)
        {
            int tag = MiscUtilsInternal.DeserializeInt32BE(
                data, offset);
            if (tag <= 0)
            {
                throw new KabomuIOException("invalid tag: " +
                    tag);
            }
            return tag;
        }

        /// <summary>
        /// Decodes a 4-byte buffer slice into a length.
        /// </summary>
        /// <param name="data">source buffer</param>
        /// <param name="offset">starting position in source buffer</param>
        /// <returns>decoded length</returns>
        /// <exception cref="ArgumentException">The decoded length is
        /// negative.</exception>
        public static int DecodeLength(byte[] data, int offset)
        {
            int decodedLength = MiscUtilsInternal.DeserializeInt32BE(
                data, offset);
            if (decodedLength < 0)
            {
                throw new ArgumentException("invalid tag value length: " +
                    decodedLength);
            }
            return decodedLength;
        }

        private static int DecodeLengthObtainedFromStream(byte[] data, int offset)
        {
            int decodedLength = MiscUtilsInternal.DeserializeInt32BE(
                data, offset);
            if (decodedLength < 0)
            {
                throw new KabomuIOException("invalid tag value length: " +
                    decodedLength);
            }
            return decodedLength;
        }

        /// <summary>
        /// Reads a 4-byte tag.
        /// </summary>
        /// <param name="inputStream">source of read</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>a task whose result is a positive number representing
        /// a tag</returns>
        public static async Task<int> ReadTagOnly(Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            var encodedTag = new byte[4];
            await IOUtilsInternal.ReadBytesFully(inputStream,
                encodedTag, 0, encodedTag.Length,
                cancellationToken);
            return DecodeTagObtainedFromStream(encodedTag, 0);
        }

        /// <summary>
        /// Reads a 4-byte tag.
        /// </summary>
        /// <param name="inputStream">source of read</param>
        /// <returns>a positive number representing a tag</returns>
        public static int ReadTagOnlySync(Stream inputStream)
        {
            var encodedTag = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(inputStream,
                encodedTag, 0, encodedTag.Length);
            return DecodeTagObtainedFromStream(encodedTag, 0);
        }

        /// <summary>
        /// Reads a 4-byte length.
        /// </summary>
        /// <param name="inputStream">source of read</param>
        /// <param name="cancellationToken">
        /// The optional token to monitor for cancellation requests.</param>
        /// <returns>a task whose result is a non-negative number representing
        /// a length</returns>
        public static async Task<int> ReadLengthOnly(Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            var encodedLen = new byte[4];
            await IOUtilsInternal.ReadBytesFully(inputStream,
                encodedLen, 0, encodedLen.Length,
                cancellationToken);
            return DecodeLengthObtainedFromStream(encodedLen, 0);
        }

        /// <summary>
        /// Reads a 4-byte length.
        /// </summary>
        /// <param name="inputStream">source of read</param>
        /// <returns>a non-negative number representing a length</returns>
        public static int ReadLengthOnlySync(Stream inputStream)
        {
            var encodedLen = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(inputStream,
                encodedLen, 0, encodedLen.Length);
            return DecodeLengthObtainedFromStream(encodedLen, 0);
        }

        /// <summary>
        /// Creates a stream which wraps another stream to
        /// ensure that a given amount of bytes are read from it.
        /// </summary>
        /// <param name="stream">the readable stream to read from</param>
        /// <param name="length">the expected number of bytes to read from stream
        /// argument. Must not be negative.</param>
        /// <returns>stream which enforces a certain length on
        /// readable stream argument</returns>
        public static Stream CreateContentLengthEnforcingStream(Stream stream,
            long length)
        {
            return new ContentLengthEnforcingStreamInternal(stream, length);
        }

        /// <summary>
        /// Creates a stream which wraps another stream to ensure that
        /// a given amount of bytes are not exceeded when reading from it.
        /// </summary>
        /// <param name="stream">the readable stream to read from</param>
        /// <param name="maxLength">the number of bytes beyond which
        /// reads will fail. Can be zero, in which case a default of 128MB
        /// will be used.</param>
        /// <returns>stream which enforces a maximum length on readable
        /// stream argument.</returns>
        public static Stream CreateMaxLengthEnforcingStream(Stream stream,
            int maxLength = 0)
        {
            return new MaxLengthEnforcingStreamInternal(stream, maxLength);
        }

        /// <summary>
        /// Creates a stream which wraps another stream to decode
        /// TLV-encoded byte chunks from it.
        /// </summary>
        /// <param name="stream">the readable stream to read from</param>
        /// <param name="expectedTag">the tag of the byte chunks</param>
        /// <param name="tagToIgnore">the tag of any optional byte chunk
        /// preceding chunks with the expected tag.</param>
        /// <returns>stream which decodes TLV-encoded bytes chunks.</returns>
        public static Stream CreateTlvDecodingReadableStream(Stream stream,
            int expectedTag, int tagToIgnore)
        {
            return new BodyChunkDecodingStreamInternal(stream, expectedTag,
                tagToIgnore);
        }

        /// <summary>
        /// Creates a stream which wraps another stream to encode byte chunks
        /// into it in TLV format.
        /// </summary>
        /// <param name="stream">the writable stream to write to</param>
        /// <param name="tagToUse">the tag to use to encode byte chunks</param>
        /// <returns>stream which encodes byte chunks in TLV format</returns>
        public static Stream CreateTlvEncodingWritableStream(Stream stream,
            int tagToUse)
        {
            return new BodyChunkEncodingStreamInternal(stream, tagToUse);
        }
    }
}
