using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class BodyChunkCodecStreamsInternalTest
    {
        [InlineData("", 1)]
        [InlineData("a", 4)]
        [InlineData("ab", 45)]
        [InlineData("abc", 60)]
        [InlineData("abcd", 120_000_000)]
        [InlineData("abcde", 34_000_000)]
        [InlineData("abcdefghi", 0x3245671d)]
        [Theory]
        public async Task TestReading(string expected, int tagToUse)
        {
            // 1. arrange
            Stream srcStream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            var destStream = new MemoryStream();
            var encodingStream = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            await srcStream.CopyToAsync(encodingStream);
            await TlvUtils.WriteEndOfTlvStream(destStream, tagToUse);
            destStream.Position = 0; // reset for reading.
            var decodingStream = TlvUtils.CreateTlvDecodingReadableStream(
                destStream, tagToUse, 0);
            var actual = await ComparisonUtils.ReadToString(decodingStream,
                false);

            // assert
            Assert.Equal(expected, actual);

            // 2. arrange again with old style async
            srcStream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            destStream = new MemoryStream();
            encodingStream = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            await srcStream.CopyToAsync(encodingStream);
            await TlvUtils.WriteEndOfTlvStream(destStream, tagToUse);
            destStream.Position = 0; // reset for reading.
            decodingStream = TlvUtils.CreateTlvDecodingReadableStream(
                destStream, tagToUse, 0);
            actual = await ComparisonUtils.ReadToString(decodingStream,
                true);

            // assert
            Assert.Equal(expected, actual);

            // 3. arrange again with sync
            srcStream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            destStream = new MemoryStream();
            encodingStream = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            srcStream.CopyTo(encodingStream);
            destStream.Write(TlvUtils.EncodeTagAndLengthOnly(tagToUse, 0));
            destStream.Position = 0; // reset for reading.
            decodingStream = TlvUtils.CreateTlvDecodingReadableStream(
                destStream, tagToUse, 0);
            actual = ComparisonUtils.ReadToStringSync(decodingStream,
                false);

            // assert
            Assert.Equal(expected, actual);

            // 4. arrange again with slow sync
            srcStream = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            destStream = new MemoryStream();
            encodingStream = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            srcStream.CopyTo(encodingStream);
            destStream.Write(TlvUtils.EncodeTagAndLengthOnly(tagToUse, 0));
            destStream.Position = 0; // reset for reading.
            decodingStream = TlvUtils.CreateTlvDecodingReadableStream(
                destStream, tagToUse, 0);
            actual = ComparisonUtils.ReadToStringSync(decodingStream,
                true);

            // assert
            Assert.Equal(expected, actual);
        }
    }
}
