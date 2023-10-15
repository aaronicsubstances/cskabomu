using Kabomu.Tlv;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Tlv
{
    public class TlvUtilsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestEncodeTagData))]
        public void TestEncodeTag(int tag, byte[] dest, int offset,
            byte[] expected)
        {
            byte[] actual = new byte[dest.Length];
            Array.Copy(dest, actual, dest.Length);
            TlvUtils.EncodeTag(tag, actual, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestEncodeTagData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    0x15c0,
                    new byte[]{ 1, 1, 1, 2, 2, 2, 3 },
                    2,
                    new byte[] { 1, 1, 0, 0, 0x15, 0xc0, 3 }
                },
                new object[]
                {
                    0x12342143,
                    new byte[5],
                    0,
                    new byte[] { 0x12, 0x34, 0x21, 0x43, 0 }
                },
                new object[]
                {
                    1,
                    new byte[]{ 3, 2, 4, 5, 187, 9 },
                    1,
                    new byte[] { 3, 0, 0, 0, 1, 9 }
                }
            };
        }

        [Fact]
        public void TestEncodeTagForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTag(10, new byte[4], 1));
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTag(-1, new byte[5], 1));
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTag(0, new byte[4], 0));
        }

        [Theory]
        [MemberData(nameof(CreateTestEncodeLengthData))]
        public void TestEncodeLength(int length, byte[] dest, int offset,
            byte[] expected)
        {
            byte[] actual = new byte[dest.Length];
            Array.Copy(dest, actual, dest.Length);
            TlvUtils.EncodeLength(length, actual, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestEncodeLengthData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    0x34,
                    new byte[4],
                    0,
                    new byte[] { 0, 0, 0, 0x34 }
                },
                new object[]
                {
                    0,
                    new byte[]{ 2, 3, 2, 3, 4 },
                    1,
                    new byte[] { 2, 0, 0, 0, 0 }
                },
                new object[]
                {
                    0x78cdef01,
                    new byte[] { 0, 0, 0, 1,
                        0x78, 0xcd, 0xef, 1 },
                    2,
                    new byte[] { 0, 0, 0x78, 0xcd,
                        0xef, 1, 0xef, 1 }
                }
            };
        }

        [Fact]
        public void TestEncodeLengthForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeLength(10, new byte[3], 0));
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeLength(-1, new byte[5], 1));
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeTagData))]
        public void TestDecodeTag(byte[] data, int offset,
            int expected)
        {
            var actual = TlvUtils.DecodeTag(data, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDecodeTagData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    new byte[] { 0, 0, 0, 1 },
                    0,
                    1
                },
                new object[]
                {
                    new byte[] { 0x03, 0x40, 0x89, 0x11 },
                    0,
                    0x03408911
                },
                new object[]
                {
                    new byte[] { 1, 0x56, 0x10, 0x01, 0x20, 2 },
                    1,
                    0x56100120
                }
            };
        }

        [Fact]
        public void TestDecodeTagForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.DecodeTag(new byte[] { 1, 1, 1 }, 0));
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.DecodeTag(new byte[4], 0));
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.DecodeTag(new byte[] { 5, 1, 200, 3, 0, 3 }, 2));
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeLengthData))]
        public void TestDecodeLength(byte[] data, int offset, int expected)
        {
            var actual = TlvUtils.DecodeLength(data, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDecodeLengthData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    new byte[] { 0, 0, 0, 0 },
                    0,
                    0
                },
                new object[]
                {
                    new byte[] { 1, 2, 0, 0, 0, 1 },
                    2,
                    1
                },
                new object[]
                {
                    new byte[] { 0x03, 0x40, 0x89, 0x11 },
                    0,
                    0x03408911
                },
                new object[]
                {
                    new byte[] { 1, 0x56, 0x10, 0x01, 0x20, 2 },
                    1,
                    0x56100120
                }
            };
        }

        [Fact]
        public void TestDecodeLengthForErrors()
        {
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.DecodeLength(new byte[] { 1, 1, 1 }, 0));
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.DecodeLength(new byte[] { 5, 1, 200, 3, 0, 3 }, 2));
        }

        /// <summary>
        /// NB: Test method only tests with one and zero,
        /// so as to guarantee that data will not be split,
        /// even when test is ported to other languages.
        /// </summary>
        [Fact]
        public async Task TestCreateTlvEncodingWritableStream()
        {
            // arrange
            byte srcByte = 45;
            var tagToUse = 16;
            var expected = new byte[]
            {
                0, 0, 0, 16,
                0, 0, 0, 1,
                45,
                0, 0, 0, 16,
                0, 0, 0, 0
            };
            var destStream = new MemoryStream();
            var instance = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            await instance.WriteAsync(new byte[] { srcByte });
            await instance.WriteAsync(new byte[0]);
            // write end of stream
            await instance.WriteAsync(null, 0, -1);

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);

            // test with sync

            // arrange
            srcByte = 145;
            tagToUse = 0x79452316;
            expected = new byte[]
            {
                0x79, 0x45, 0x23, 0x16,
                0, 0, 0, 1,
                145,
                0x79, 0x45, 0x23, 0x16,
                0, 0, 0, 0
            };
            destStream = new MemoryStream();
            instance = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            instance.Write(new byte[] { srcByte });
            instance.Write(new byte[0]);
            // write end of stream
            instance.Write(null, 0, -1);

            // assert
            actual = destStream.ToArray();
            Assert.Equal(expected, actual);

            // test with slow sync

            // arrange
            srcByte = 78;
            tagToUse = 0x3cd456;
            expected = new byte[]
            {
                0, 0x3c, 0xd4, 0x56,
                0, 0, 0, 1,
                78
            };
            destStream = new MemoryStream();
            instance = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            instance.WriteByte(srcByte);

            // assert
            actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestCreateTlvEncodingWritableStreamForCancellation()
        {
            // 1. arrange
            var stream = new MemoryStream();
            var instance = TlvUtils.CreateTlvEncodingWritableStream(stream,
                5);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await instance.WriteAsync(new byte[2], cts.Token));
        }

        [InlineData("", 1)]
        [InlineData("a", 4)]
        [InlineData("ab", 45)]
        [InlineData("abc", 60)]
        [InlineData("abcd", 120_000_000)]
        [InlineData("abcde", 34_000_000)]
        [InlineData("abcdefghi", 0x3245671d)]
        [Theory]
        public async Task TestBodyChunkCodecStreams(string expected, int tagToUse)
        {
            // 1. arrange
            Stream srcStream = new RandomizedReadInputStream(expected);
            var destStream = new MemoryStream();
            var encodingStream = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            await srcStream.CopyToAsync(encodingStream);
            // write end of stream
            await encodingStream.WriteAsync(null, 0, -1);
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
            // write end of stream
            await encodingStream.WriteAsync(null, 0, -1);
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
            // write end of stream
            encodingStream.Write(null, 0, -1);
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
            // write end of stream
            encodingStream.Write(null, 0, -1);
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
