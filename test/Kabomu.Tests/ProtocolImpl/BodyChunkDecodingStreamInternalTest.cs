using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class BodyChunkDecodingStreamInternalTest
    {
        [Theory]
        [MemberData(nameof(CreateTestReadingData))]
        public async Task TestReading(byte[] srcData,
            int expectedTag, int tagToIgnore, byte[] expected)
        {
            // arrange
            Stream instance = new RandomizedReadInputStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(
                instance, expectedTag, tagToIgnore);

            // act
            var actual = await ComparisonUtils.ReadToBytes(instance, false);

            // assert
            Assert.Equal(expected, actual);

            // 2. arrange again with old style async
            instance = new RandomizedReadInputStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(
                instance, expectedTag, tagToIgnore);

            // act
            actual = await ComparisonUtils.ReadToBytes(instance, true);

            // assert
            Assert.Equal(expected, actual);

            // 3. arrange again with sync
            instance = new RandomizedReadInputStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(
                instance, expectedTag, tagToIgnore);

            // act
            actual = ComparisonUtils.ReadToBytesSync(instance, false);

            // assert
            Assert.Equal(expected, actual);

            // 4. arrange again with slow sync
            instance = new RandomizedReadInputStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(
                instance, expectedTag, tagToIgnore);

            // act
            actual = ComparisonUtils.ReadToBytesSync(instance, true);

            // assert
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestReadingData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[]
            {
                0, 0, 0, 89,
                0, 0, 0, 0
            };
            int expectedTag = 89;
            int tagToIgnore = 5;
            var expected = new byte[] { };
            testData.Add(new object[] { srcData, expectedTag, tagToIgnore,
                expected });

            srcData = new byte[]
            {
                0, 0, 0, 15,
                0, 0, 0, 2,
                2, 3,
                0, 0, 0, 8,
                0, 0, 0, 0
            };
            expectedTag = 8;
            tagToIgnore = 15;
            expected = new byte[] { };
            testData.Add(new object[] { srcData, expectedTag, tagToIgnore,
                expected });

            srcData = new byte[]
            {
                0, 0, 0, 8,
                0, 0, 0, 2,
                2, 3,
                0, 0, 0, 8,
                0, 0, 0, 0
            };
            expectedTag = 8;
            tagToIgnore = 15;
            expected = new byte[] { 2, 3 };
            testData.Add(new object[] { srcData, expectedTag, tagToIgnore,
                expected });

            srcData = new byte[]
            {
                0, 0, 0, 8,
                0, 0, 0, 1,
                2,
                0, 0, 0, 8,
                0, 0, 0, 1,
                3,
                0, 0, 0, 8,
                0, 0, 0, 0
            };
            expectedTag = 8;
            tagToIgnore = 15;
            expected = new byte[] { 2, 3 };
            testData.Add(new object[] { srcData, expectedTag, tagToIgnore,
                expected });

            srcData = new byte[]
            {
                0, 0, 0x3d, 0x15,
                0, 0, 0, 0,
                0x30, 0xa3, 0xb5, 0x17,
                0, 0, 0, 1,
                2,
                0, 0, 0x3d, 0x15,
                0, 0, 0, 7,
                0, 0, 0, 0, 0, 0, 0,
                0x30, 0xa3, 0xb5, 0x17,
                0, 0, 0, 1,
                3,
                0, 0, 0x3d, 0x15,
                0, 0, 0, 0,
                0x30, 0xa3, 0xb5, 0x17,
                0, 0, 0, 4,
                2, 3, 45, 62,
                0, 0, 0x3d, 0x15,
                0, 0, 0, 1,
                1,
                0x30, 0xa3, 0xb5, 0x17,
                0, 0, 0, 8,
                91, 100, 2, 3, 45, 62, 70, 87,
                0x30, 0xa3, 0xb5, 0x17,
                0, 0, 0, 0
            };
            expectedTag = 0x30a3b517;
            tagToIgnore = 0x3d15;
            expected = new byte[] { 2, 3, 2, 3, 45, 62,
                91, 100, 2, 3, 45, 62, 70, 87 };
            testData.Add(new object[] { srcData, expectedTag, tagToIgnore,
                expected });

            return testData;
        }

        [Fact]
        public async Task TestForCancellation()
        {
            // 1. arrange
            var stream = new MemoryStream();
            var instance = TlvUtils.CreateTlvDecodingReadableStream(stream,
                5, 3);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await instance.ReadAsync(new byte[2], cts.Token));
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodingForErrorsData))]
        public async void TestDecodingForErrors(byte[] srcData, int expectedTag,
            int tagToIgnore, string expected)
        {
            // arrange
            Stream instance = new MemoryStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(instance,
                expectedTag, tagToIgnore);

            // act
            var actualEx = await Assert.ThrowsAsync<KabomuIOException>(async () =>
            {
                await ComparisonUtils.ReadToString(instance);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);

            // 2. arrange again with old style async
            instance = new MemoryStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(instance,
                expectedTag, tagToIgnore);

            // act
            actualEx = await Assert.ThrowsAsync<KabomuIOException>(async () =>
            {
                await ComparisonUtils.ReadToString(instance, true);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);

            // 3. arrange again with sync
            instance = new MemoryStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(instance,
                expectedTag, tagToIgnore);

            // act
            actualEx = Assert.Throws<KabomuIOException>(() =>
            {
                ComparisonUtils.ReadToStringSync(instance);
            });

            // assert
            Assert.Contains(expected, actualEx.Message);

            // 4. arrange again with slow sync
            instance = new MemoryStream(srcData);
            instance = TlvUtils.CreateTlvDecodingReadableStream(instance,
                expectedTag, tagToIgnore);

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
            var testData = new List<object[]>();

            byte[] srcData = new byte[]
            {
                0, 0, 0x09, 0,
                0, 0, 0, 12
            };
            int expectedTag = 0x0900;
            int tagToIgnore = 0;
            string expected = "unexpected end of read";
            testData.Add(new object[] { srcData, expectedTag,
                tagToIgnore, expected });

            srcData = new byte[]
            {
                0, 0, 0x09, 0,
                0, 0, 0, 12
            };
            expectedTag = 10;
            tagToIgnore = 30;
            expected = "unexpected tag";
            testData.Add(new object[] { srcData, expectedTag,
                tagToIgnore, expected });

            srcData = new byte[]
            {
                0, 0, 0, 15,
                0, 0, 0, 2,
                2, 3,
                0, 0, 0, 15,
                0, 0, 0, 2,
                2, 3,
                0, 0, 0, 8,
                0, 0, 0, 0
            };
            expectedTag = 8;
            tagToIgnore = 15;
            expected = "unexpected tag";
            testData.Add(new object[] { srcData, expectedTag,
                tagToIgnore, expected });

            srcData = new byte[]
            {
                0, 0, 0, 0,
                0, 0xff, 0xff, 0xec,
                2, 3,
                0, 0, 0, 14,
                0, 0, 0, 0,
                2, 3,
                0, 0, 0, 8,
                0, 0, 0, 0
            };
            expectedTag = 14;
            tagToIgnore = 8;
            expected = "invalid tag: 0";
            testData.Add(new object[] { srcData, expectedTag,
                tagToIgnore, expected });

            srcData = new byte[]
            {
                0, 0, 0, 14,
                0xff, 0xff, 0xff, 0xec,
                2, 3,
                0, 0, 0, 14,
                0, 0, 0, 0,
                2, 3,
                0, 0, 0, 8,
                0, 0, 0, 0
            };
            expectedTag = 14;
            tagToIgnore = 15;
            expected = "invalid tag value length: -20";
            testData.Add(new object[] { srcData, expectedTag,
                tagToIgnore, expected });

            return testData;
        }
    }
}
