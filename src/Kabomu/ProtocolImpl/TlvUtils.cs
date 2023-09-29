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
            MiscUtilsInternal.SerializeInt32BE(tag, tagAndLen, 0, 4);
            MiscUtilsInternal.SerializeInt32BE(length, tagAndLen, 4, 4);
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
                encodedTag, 0, encodedTag.Length);
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
                encodedTag, 0, encodedTag.Length);
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
                encodedLen, 0, encodedLen.Length);
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
                encodedLen, 0, encodedLen.Length);
            if (decodedLength < 0)
            {
                throw new KabomuIOException("invalid tag value length: " +
                    decodedLength);
            }
            return decodedLength;
        }

        public static async Task ReadAwayUntilTag(Stream inputStream,
            int stoppageTag,
            CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var tag = await ReadTagOnly(
                    inputStream, cancellationToken);
                if (tag != stoppageTag)
                {
                    int lengthToSkip = await ReadLengthOnly(
                        inputStream, cancellationToken);
                    if (lengthToSkip > 0)
                    {
                        await GetCurrentValueAsStream(inputStream,
                            lengthToSkip).CopyToAsync(Stream.Null, cancellationToken);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public static void ReadAwayUntilTagSync(Stream inputStream,
            int stoppageTag)
        {
            while (true)
            {
                var tag = ReadTagOnlySync(inputStream);
                if (tag != stoppageTag)
                {
                    int lengthToSkip = ReadLengthOnlySync(inputStream);
                    if (lengthToSkip > 0)
                    {
                        GetCurrentValueAsStream(inputStream,
                            lengthToSkip).CopyTo(Stream.Null);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public static Stream GetCurrentValueAsStream(Stream 
            inputStream, int length)
        {
            if (length != 0)
            {
                return new ContentLengthEnforcingStreamInternal(inputStream,
                    length);
            }
            return Stream.Null;
        }
    }
}
