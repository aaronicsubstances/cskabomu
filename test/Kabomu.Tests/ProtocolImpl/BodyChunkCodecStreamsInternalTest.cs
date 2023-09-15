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
        [InlineData("")]
        [InlineData("a")]
        [InlineData("ab")]
        [InlineData("abc")]
        [InlineData("abcd")]
        [InlineData("abcde")]
        [InlineData("abcdefghi")]
        [Theory]
        public async Task TestReading(string expected)
        {
            // 1. arrange
            Stream instance = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            instance = new BodyChunkEncodingStreamInternal(instance);
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            var actual = await ComparisonUtils.ReadToString(instance,
                false);

            // assert
            Assert.Equal(expected, actual);

            // 2. arrange again with old style async
            instance = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            instance = new BodyChunkEncodingStreamInternal(instance);
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            actual = await ComparisonUtils.ReadToString(instance,
                true);

            // assert
            Assert.Equal(expected, actual);

            // 3. arrange again with sync
            instance = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            instance = new BodyChunkEncodingStreamInternal(instance);
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            actual = ComparisonUtils.ReadToStringSync(instance,
                false);

            // assert
            Assert.Equal(expected, actual);

            // 4. arrange again with slow sync
            instance = new RandomizedReadInputStream(
                MiscUtilsInternal.StringToBytes(expected));
            instance = new BodyChunkEncodingStreamInternal(instance);
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            actual = ComparisonUtils.ReadToStringSync(instance,
                true);

            // assert
            Assert.Equal(expected, actual);
        }
    }
}
