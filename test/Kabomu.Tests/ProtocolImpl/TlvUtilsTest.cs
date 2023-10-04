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
        public void TestEncodeTagForErrors1()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTag(10, new byte[4], 1));
        }

        [Fact]
        public void TestEncodeTagForErrors2()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeTag(-1, new byte[5], 1));
        }

        [Fact]
        public void TestEncodeTagForErrors3()
        {
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
        public void TestEncodeLengthForErrors1()
        {
            Assert.Throws<ArgumentException>(() =>
                TlvUtils.EncodeLength(10, new byte[3], 0));
        }

        [Fact]
        public void TestEncodeLengthForErrors2()
        {
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
    }
}
