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
    public static class TlvUtils
    {
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

        public static async Task<int> ReadTagOnly(Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            var encodedTag = new byte[4];
            await IOUtilsInternal.ReadBytesFully(inputStream,
                encodedTag, 0, encodedTag.Length,
                cancellationToken);
            int tag = MiscUtilsInternal.DeserializeInt32BE(
                encodedTag, 0);
            if (tag <= 0)
            {
                throw new KabomuIOException("invalid tag: " +
                    tag);
            }
            return tag;
        }

        public static int ReadTagOnlySync(Stream inputStream)
        {
            var encodedTag = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(inputStream,
                encodedTag, 0, encodedTag.Length);
            int tag = MiscUtilsInternal.DeserializeInt32BE(
                encodedTag, 0);
            if (tag <= 0)
            {
                throw new KabomuIOException("invalid tag: " +
                    tag);
            }
            return tag;
        }

        public static async Task ReadExpectedTagOnly(Stream inputStream,
            int expectedTag, CancellationToken cancellationToken)
        {
            var tag = await ReadTagOnly(
                inputStream, cancellationToken);
            if (tag != expectedTag)
            {
                throw new KabomuIOException("unexpected tag: expected " +
                    $"{expectedTag} but found {tag}");
            }
        }

        public static void ReadExpectedTagOnlySync(Stream inputStream,
            int expectedTag)
        {
            var tag = ReadTagOnlySync(inputStream);
            if (tag != expectedTag)
            {
                throw new KabomuIOException("unexpected tag: expected " +
                    $"{expectedTag} but found {tag}");
            }
        }

        public static async Task<int> ReadLengthOnly(Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            var encodedLen = new byte[4];
            await IOUtilsInternal.ReadBytesFully(inputStream,
                encodedLen, 0, encodedLen.Length,
                cancellationToken);
            int decodedLength = MiscUtilsInternal.DeserializeInt32BE(
                encodedLen, 0);
            if (decodedLength < 0)
            {
                throw new KabomuIOException("invalid tag value length: " +
                    decodedLength);
            }
            return decodedLength;
        }

        public static int ReadLengthOnlySync(Stream inputStream)
        {
            var encodedLen = new byte[4];
            IOUtilsInternal.ReadBytesFullySync(inputStream,
                encodedLen, 0, encodedLen.Length);
            int decodedLength = MiscUtilsInternal.DeserializeInt32BE(
                encodedLen, 0);
            if (decodedLength < 0)
            {
                throw new KabomuIOException("invalid tag value length: " +
                    decodedLength);
            }
            return decodedLength;
        }

        public static Stream CreateLengthEnforcingStream(Stream stream,
            long length)
        {
            if (length != 0)
            {
                return new LengthEnforcingStreamInternal(stream, length);
            }
            else
            {
                return Stream.Null;
            }
        }

        public static Stream CreateMaxLengthEnforcingStream(Stream stream,
            int maxLength = 0)
        {
            return new MaxLengthEnforcingStreamInternal(stream, maxLength);
        }

        public static Stream CreateTlvReadableStream(Stream stream,
            int expectedTag)
        {
            return new BodyChunkDecodingStreamInternal(stream, expectedTag);
        }

        public static Stream CreateTlvWritableStream(Stream stream,
            int tagToUse)
        {
            return new BodyChunkEncodingStreamInternal(stream, tagToUse);
        }

        public static async Task WriteEndOfTlvStream(Stream stream,
            int tagToUse,
            CancellationToken cancellationToken = default)
        {
            await stream.WriteAsync(
                EncodeTagAndLengthOnly(tagToUse, 0),
                cancellationToken);
        }
    }
}
