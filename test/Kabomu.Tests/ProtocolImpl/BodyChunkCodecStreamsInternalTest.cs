using Kabomu.Exceptions;
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
    /*public class BodyChunkCodecStreamsInternalTest
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

        [Theory]
        [MemberData(nameof(CreateTestDecodingForErrorsData))]
        public async void TestDecodingForErrors(string srcData, string expected)
        {
            // arrange
            Stream instance = new MemoryStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            var actualEx = await Assert.ThrowsAsync<KabomuIOException>(async () =>
            {
                await ComparisonUtils.ReadToString(instance);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);

            // 2. arrange again with old style async
            instance = new MemoryStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            actualEx = await Assert.ThrowsAsync<KabomuIOException>(async () =>
            {
                await ComparisonUtils.ReadToString(instance, true);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);

            // 3. arrange again with sync
            instance = new MemoryStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            actualEx = Assert.Throws<KabomuIOException>(() =>
            {
                ComparisonUtils.ReadToStringSync(instance);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);

            // 4. arrange again with slow sync
            instance = new MemoryStream(
                MiscUtilsInternal.StringToBytes(srcData));
            instance = new BodyChunkDecodingStreamInternal(instance);

            // act
            actualEx = Assert.Throws<KabomuIOException>(() =>
            {
                ComparisonUtils.ReadToStringSync(instance, true);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);
        }

        public static List<object[]> CreateTestDecodingForErrorsData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    "",
                    "unexpected end of read"
                },
                new object[]
                {
                    "01",
                    "unexpected end of read"
                },
                new object[]
                {
                    "01,0000000001qabc,",
                    "unexpected end of read"
                },
                new object[]
                {
                    "01,0000000012qabc,",
                    "unexpected end of read"
                },
                new object[]
                {
                    "01234567890123456",
                    "invalid quasi http body chunk header"
                },
                new object[]
                {
                    "01,0000000001h00,234567890123456",
                    "invalid quasi http body chunk header"
                },
                new object[]
                {
                    "01,0000000tea",
                    "length: 0000000tea"
                },
                new object[]
                {
                    "01,-000000001",
                    "length: -1"
                }
            };
        }
    }*/
}
