using Kabomu.Exceptions;
using Kabomu.ProtocolImpl;
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
    public class TlvUtilsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestEncodeTagAndLengthOnlyData))]
        public void TestEncodeTagAndLengthOnly(int tag, int length,
            byte[] expected)
        {
            var actual = TlvUtils.EncodeTagAndLengthOnly(tag, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestEncodeTagAndLengthOnlyData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    0x15c0,
                    0x34,
                    new byte[] { 0, 0, 0x15, 0xc0,
                        0, 0, 0, 0x34 }
                },
                new object[]
                {
                    0x12342143,
                    0,
                    new byte[] { 0x12, 0x34, 0x21, 0x43,
                        0, 0, 0, 0 }
                },
                new object[]
                {
                    1,
                    0x78cdef01,
                    new byte[] { 0, 0, 0, 1,
                        0x78, 0xcd, 0xef, 1 }
                }
            };
        }

        [Fact]
        public void TestEncodeTagAndLengthOnlyForErrors1()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTagAndLengthOnly(0, 1));
        }

        [Fact]
        public void TestEncodeTagAndLengthOnlyForErrors2()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTagAndLengthOnly(-1, 1));
        }

        [Fact]
        public void TestEncodeTagAndLengthOnlyForErrors3()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTagAndLengthOnly(10, -1));
        }

        [Fact]
        public async Task TestWriteEndOfTlvStream1()
        {
            var stream = new MemoryStream();
            int tag = 5;
            CancellationToken cancellationToken = CancellationToken.None;
            var expected = new byte[] { 0, 0, 0, 5,
                0, 0, 0 ,0 };
            await TlvUtils.WriteEndOfTlvStream(stream, tag,
                cancellationToken);
            var actual = stream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriteEndOfTlvStream2()
        {
            var stream = new MemoryStream();
            int tag = 0x532ABCD2;
            CancellationToken cancellationToken = CancellationToken.None;
            var expected = new byte[] { 0X53, 0x2a, 0xbc, 0xd2,
                0, 0, 0 ,0 };
            await TlvUtils.WriteEndOfTlvStream(stream, tag,
                cancellationToken);
            var actual = stream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriteEndOfTlvStreamForErrors1()
        {
            var stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await TlvUtils.WriteEndOfTlvStream(stream, -1,
                    CancellationToken.None);
            });
        }

        [Fact]
        public async Task TestWriteEndOfTlvStreamForErrors2()
        {
            var stream = new MemoryStream();
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var cts = new CancellationTokenSource();
                cts.Cancel();
                await TlvUtils.WriteEndOfTlvStream(stream, 4,
                    cts.Token);
            });
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
        public void TestDecodeTagForErrors1()
        {
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.DecodeTag(new byte[] { 1, 1, 1 }, 0));
        }

        [Fact]
        public void TestDecodeTagForErrors2()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.DecodeTag(new byte[4], 0));
        }

        [Fact]
        public void TestDecodeTagForErrors3()
        {
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
        public void TestDecodeLengthForErrors1()
        {
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.DecodeLength(new byte[] { 1, 1, 1 }, 0));
        }

        [Fact]
        public void TestDecodeLengthForErrors2()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.DecodeLength(new byte[] { 5, 1, 200, 3, 0, 3 }, 2));
        }

        [Theory]
        [MemberData(nameof(CreateTestReadTagOnlyData))]
        public async Task TestReadTagOnly(byte[] src, int expected)
        {
            // start with async.
            var stream = new MemoryStream(src);
            var cancellationToken = CancellationToken.None;
            var actual = await TlvUtils.ReadTagOnly(stream, cancellationToken);
            Assert.Equal(expected, actual);

            // repeat without async.
            stream = new MemoryStream(src);
            actual = TlvUtils.ReadTagOnlySync(stream);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestReadTagOnlyData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    new byte[]{ 0, 0, 0, 1 },
                    1
                },
                new object[]
                {
                    new byte[]{ 0, 0, 0, 27 },
                    27
                },
                new object[]
                {
                    new byte[]{ 0, 0, 0x60, 0x21 },
                    0x6021
                },
                new object[]
                {
                    new byte[] { 0x03, 0x40, 0x89, 0x11 },
                    0x03408911
                },
            };
        }

        [Fact]
        public async Task TestReadTagOnlyForErrors1()
        {
            var bytes = new byte[3];
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await TlvUtils.ReadTagOnly(new MemoryStream(bytes)));
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.ReadTagOnlySync(new MemoryStream(bytes)));
        }

        [Fact]
        public async Task TestReadTagOnlyForErrors2()
        {
            var bytes = new byte[4];
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await TlvUtils.ReadTagOnly(new MemoryStream(bytes)));
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.ReadTagOnlySync(new MemoryStream(bytes)));
        }

        [Fact]
        public async Task TestReadTagOnlyForErrors3()
        {
            var bytes = new byte[] { 203, 40, 120, 170 };
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await TlvUtils.ReadTagOnly(new MemoryStream(bytes)));
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.ReadTagOnlySync(new MemoryStream(bytes)));
        }

        [Fact]
        public async Task TestReadTagOnlyForErrors4()
        {
            var bytes = new byte[] { 203, 40, 120, 170 };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await TlvUtils.ReadTagOnly(new MemoryStream(bytes),
                    cts.Token));
        }

        [Theory]
        [MemberData(nameof(CreateTestReadLengthOnlyData))]
        public async Task TestReadLengthOnly(byte[] src, int expected)
        {
            // start with async.
            var stream = new MemoryStream(src);
            var cancellationToken = CancellationToken.None;
            var actual = await TlvUtils.ReadLengthOnly(stream, cancellationToken);
            Assert.Equal(expected, actual);

            // repeat without async.
            stream = new MemoryStream(src);
            actual = TlvUtils.ReadLengthOnlySync(stream);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestReadLengthOnlyData()
        {
            return new List<object[]>
            {
                new object[]
                {
                    new byte[]{ 0, 0, 0, 0 },
                    0
                },
                new object[]
                {
                    new byte[]{ 0, 0, 0, 1 },
                    1
                },
                new object[]
                {
                    new byte[]{ 0, 0, 0, 117 },
                    117
                },
                new object[]
                {
                    new byte[]{ 0, 0, 0x60, 0x21 },
                    0x6021
                },
                new object[]
                {
                    new byte[] { 0x03, 0x40, 0x89, 0x11 },
                    0x03408911
                },
            };
        }

        [Fact]
        public async Task TestReadLengthOnlyForErrors1()
        {
            var bytes = new byte[3];
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await TlvUtils.ReadLengthOnly(new MemoryStream(bytes)));
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.ReadLengthOnlySync(new MemoryStream(bytes)));
        }

        [Fact]
        public async Task TestReadLengthOnlyForErrors2()
        {
            var bytes = new byte[] { 203, 40, 120, 170 };
            await Assert.ThrowsAnyAsync<Exception>(async () =>
                await TlvUtils.ReadLengthOnly(new MemoryStream(bytes)));
            Assert.ThrowsAny<Exception>(() =>
                TlvUtils.ReadLengthOnlySync(new MemoryStream(bytes)));
        }

        [Fact]
        public async Task TestReadLengthOnlyForErrors3()
        {
            var bytes = new byte[] { 203, 40, 120, 170 };
            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await TlvUtils.ReadLengthOnly(new MemoryStream(bytes),
                    cts.Token));
        }
    }
}
