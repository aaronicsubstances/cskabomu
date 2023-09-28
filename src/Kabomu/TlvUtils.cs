using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    public static class TlvUtils
    {
        internal const int MaxAllowableTagValueLength = 999_999_999;

        public static byte[] EncodeTagLengthOnly(byte tag,
            int length)
        {
            if (length < 0 || length > MaxAllowableTagValueLength)
            {
                throw new ArgumentException("invalid tag value length: " +
                    length);
            }
            var tagAndLen = new byte[10];
            tagAndLen[0] = tag;
            var encodedLength = MiscUtilsInternal.StringToBytes(
                $"{length}");
            Array.Copy(encodedLength, 0, tagAndLen, 1,
                encodedLength.Length);
            return tagAndLen;
        }

        public static async Task WriteTlv(Stream outputStream,
            byte tag, byte[] value,
            CancellationToken cancellationToken = default)
        {
            var tagAndLen = EncodeTagLengthOnly(tag, value.Length);
            await outputStream.WriteAsync(tagAndLen, cancellationToken);
            await outputStream.WriteAsync(value, cancellationToken);
        }

        public static async Task<int> ReadTagAndLengthOnly(
            Stream inputStream, byte expectedTag,
            CancellationToken cancellationToken = default)
        {
            var tagAndLen = new byte[10];
            await IOUtilsInternal.ReadBytesFully(inputStream,
                tagAndLen, 0, tagAndLen.Length, cancellationToken);
            return CompleteReadTagAndLengthOnly(tagAndLen, expectedTag);
        }

        public static int ReadTagAndLengthOnlySync(
            Stream inputStream, byte expectedTag)
        {
            var tagAndLen = new byte[10];
            IOUtilsInternal.ReadBytesFullySync(inputStream,
                tagAndLen, 0, tagAndLen.Length);
            return CompleteReadTagAndLengthOnly(tagAndLen, expectedTag);
        }

        private static int CompleteReadTagAndLengthOnly(
            byte[] tagAndLen, byte expectedTag)
        {
            if (tagAndLen[0] != expectedTag)
            {
                throw new KabomuIOException("unexpected tag: expected " +
                    $"{expectedTag} but found {tagAndLen[0]}");
            }
            var decodedLength = MiscUtilsInternal.ParseInt32(
                MiscUtilsInternal.BytesToString(tagAndLen, 1, tagAndLen.Length - 1));
            if (decodedLength < 0)
            {
                throw new KabomuIOException("invalid tag length: " +
                    decodedLength);
            }
            return decodedLength;
        }

        public static async Task<byte[]> ReadTlv(Stream inputStream,
            byte expectedTag, int expectedLength,
            CancellationToken cancellationToken = default)
        {
            var decodedLength = await ReadTagAndLengthOnly(inputStream,
                expectedTag, cancellationToken);
            if (decodedLength != expectedLength)
            {
                throw new KabomuIOException("unexpected length: expected " +
                    $"{expectedLength} but found {decodedLength}");
            }
            var value = new byte[decodedLength];
            if (decodedLength > 0)
            {
                await IOUtilsInternal.ReadBytesFully(inputStream,
                    value, 0, value.Length, cancellationToken);
            }
            return value;
        }
    }
}
