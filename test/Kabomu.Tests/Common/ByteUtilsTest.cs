using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class ByteUtilsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestIsValidByteBufferSliceData))]
        public void TestIsValidByteBufferSlice(byte[] data, int offset, int length, bool expected)
        {
            bool actual = ByteUtils.IsValidByteBufferSlice(data, offset, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestIsValidByteBufferSliceData()
        {
            return new List<object[]>
            {
                new object[] { null, 0, 0, false },
                new object[] { new byte[0], 0, 0, true },
                new object[] { new byte[0], 1, 0, false },
                new object[] { new byte[0], 0, 1, false },
                new object[]{ new byte[1], 0, 1, true },
                new object[]{ new byte[1], -1, 0, false },
                new object[]{ new byte[1], -1, 0, false },
                new object[]{ new byte[1], 1, 1, false },
                new object[]{ new byte[2], 1, 1, true },
                new object[]{ new byte[2], 0, 2, true },
                new object[]{ new byte[3], 2, 2, false },
            };
        }

        [Fact]
        public void TestStringToBytes()
        {
            var actual = ByteUtils.StringToBytes("");
            Assert.Equal(new byte[0], actual);

            actual = ByteUtils.StringToBytes("abc");
            Assert.Equal(new byte[] { (byte)'a', (byte)'b', (byte)'c' }, actual);

            actual = ByteUtils.StringToBytes("Foo \u00a9 bar \U0001d306 baz \u2603 qux");
            Assert.Equal(new byte[] { 0x46, 0x6f, 0x6f, 0x20, 0xc2, 0xa9, 0x20, 0x62, 0x61, 0x72, 0x20,
                0xf0, 0x9d, 0x8c, 0x86, 0x20, 0x62, 0x61, 0x7a, 0x20, 0xe2, 0x98, 0x83,
                0x20, 0x71, 0x75, 0x78 }, actual);
        }

        [Fact]
        public static void TestBytesToString()
        {
            var data = new byte[] { };
            int offset = 0;
            int length = 0;
            var expected = "";
            var actual = ByteUtils.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
            actual = ByteUtils.BytesToString(data);
            Assert.Equal(expected, actual);

            offset = 0;
            data = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            length = data.Length;
            expected = "abc";
            actual = ByteUtils.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
            actual = ByteUtils.BytesToString(data);
            Assert.Equal(expected, actual);

            offset = 1;
            data = new byte[] { 0x46, 0x6f, 0x6f, 0x20, 0xc2, 0xa9, 0x20, 0x62, 0x61, 0x72, 0x20,
                0xf0, 0x9d, 0x8c, 0x86, 0x20, 0x62, 0x61, 0x7a, 0x20, 0xe2, 0x98, 0x83,
                0x20, 0x71, 0x75, 0x78 };
            length = data.Length - 2;
            expected = "oo \u00a9 bar \U0001d306 baz \u2603 qu";
            actual = ByteUtils.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(CreateTestSerializeUpToInt32BigEndianData))]
        public void TestSerializeUpToInt64BigEndian(int v, byte[] rawBytes,
            int offset, int length, byte[] expected)
        {
            byte[] actual = new byte[rawBytes.Length];
            Array.Copy(rawBytes, actual, rawBytes.Length);
            ByteUtils.SerializeUpToInt32BigEndian(v, actual, offset, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestSerializeUpToInt32BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ 12, new byte[1], 0, 1, new byte[] { 12 } },
                new object[]{ 12, new byte[] { 8, 2 }, 0, 2, new byte[] { 0, 12 } },
                new object[]{ 2001, new byte[] { 8, 2, 3, 4 }, 1, 2, new byte[] { 8, 7, 0xd1, 4 } },
                new object[]{ 2001, new byte[] { 8, 2, 3, 4 }, 0, 3, new byte[] { 0, 7, 0xd1, 4 } },
                new object[]{ 2001, new byte[] { 8, 2, 3, 4 }, 0, 4, new byte[] { 0, 0, 7, 0xd1 } },
                new object[]{ -10_999, new byte[2], 0, 2, new byte[] { 0xd5, 9 } },
                new object[]{ -10_999, new byte[3], 0, 3, new byte[] { 0xff, 0xd5, 9 } },
                new object[]{ -10_999, new byte[4], 0, 4, new byte[] { 0xff, 0xff, 0xd5, 9 } },
                new object[]{ 35536, new byte[] { 8, 2 }, 0, 2, new byte[] { 138, 208 } },
                new object[]{ 65535, new byte[] { 0, 1, 2 }, 0, 2, new byte[] { 255, 255, 2 } },
                new object[]{ 1_000_000, new byte[4] { 10, 20, 30, 40 }, 1, 3, new byte[] { 10, 0xf, 0x42, 0x40 } },
                new object[]{ -1_000_000, new byte[3], 0, 3, new byte[] { 0xf0, 0xbd, 0xc0 } },
                new object[]{ 1_000_000, new byte[4], 0, 4, new byte[] { 0, 0xf, 0x42, 0x40 } },
                new object[]{ 1_000_000_000, new byte[] { 10, 20, 30, 40, 50 }, 0, 4, new byte[] { 0x3b, 0x9a, 0xca, 0, 50 } },
                new object[]{ -1_000_000_000, new byte[4], 0, 4, new byte[] { 0xc4, 0x65, 0x36, 0 } },
                /*new object[]{ 2_294_967_196, new byte[] { 8, 2, 1, 0, 0, 0, 1 }, 
                    2, 4, new byte[] { 8, 2, 0x88, 0xca, 0x6b, 0x9c, 1 } },
                new object[]{ 4_294_967_295, new byte[] { 8, 2, 1, 0, 0, 0, 1 },
                    2, 4, new byte[] { 8, 2, 0xff, 0xff, 0xff, 0xff, 1 } },
                new object[]{ 72_057_594_037_927_935, new byte[] { 8, 2, 1, 0, 0, 0, 1 },
                    0, 7, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff } }*/
            };
        }

        [Fact]
        public void TestSerializeUpToInt32BigEndianForErrors()
        {
            Assert.Throws<ArgumentException>(() => ByteUtils.SerializeUpToInt32BigEndian(1, new byte[2], -1, 0));
            Assert.Throws<ArgumentException>(() => ByteUtils.SerializeUpToInt32BigEndian(2, new byte[2], 0, -1));
            Assert.Throws<ArgumentException>(() => ByteUtils.SerializeUpToInt32BigEndian(3, new byte[20], 0, 10));
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeUpToInt32BigEndianData))]
        public void TestDeserializeUpToInt32BigEndian(byte[] rawBytes,
            int offset, int length, bool signed, int expected)
        {
            var actual = ByteUtils.DeserializeUpToInt32BigEndian(rawBytes, offset, length, signed);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDeserializeUpToInt32BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 138, 208 }, 0, 2, false, 35536 },
                new object[]{ new byte[] { 255, 255, 2 }, 0, 2, false, 65535 },
                new object[]{ new byte[] { 255, 255, 2 }, 0, 2, true, -1 },
                new object[]{ new byte[] { 12 }, 0, 1, true, 12 },
                new object[]{ new byte[] { 12, 0 }, 0, 1, false, 12 },
                new object[]{ new byte[] { 0, 12 }, 0, 2, true, 12 },
                new object[]{ new byte[] { 8, 7, 0xd1, 4 }, 1, 2, true, 2001  },
                new object[]{ new byte[] { 0, 7, 0xd1, 4 }, 0, 3, false, 2001 },
                new object[]{ new byte[] { 0, 0, 7, 0xd1 }, 0, 4, true, 2001 },
                new object[]{ new byte[] { 0xd5, 9 }, 0, 2, true, -10_999 },
                new object[]{ new byte[] { 0xff, 0xd5, 9 }, 0, 3, true, -10_999 },
                new object[]{ new byte[] { 0xff, 0xff, 0xd5, 9 }, 0, 4, true, -10_999 },
                new object[]{ new byte[] { 10, 0xf, 0x42, 0x40 }, 1, 3, true, 1_000_000  },
                new object[]{ new byte[] { 0xf0, 0xbd, 0xc0 }, 0, 3, true, -1_000_000 },
                new object[]{ new byte[] { 0, 0xf, 0x42, 0x40 }, 0, 4, true, 1_000_000 },
                new object[]{ new byte[] { 0x3b, 0x9a, 0xca, 0, 50 }, 0, 4, true, 1_000_000_000 },
                new object[]{ new byte[] { 0xc4, 0x65, 0x36, 0 }, 0, 4, false, -1_000_000_000 },
                // the next would have been 2_294_967_196 if deserializing entire 32-bits as unsigned.
                new object[]{ new byte[] { 8, 2, 0x88, 0xca, 0x6b, 0x9c, 1 }, 2, 4, false, -2_000_000_100 }
            };
        }

        [Fact]
        public void TestDeserializeUpToInt32BigEndianForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
                ByteUtils.DeserializeUpToInt32BigEndian(new byte[2], -1, 0, false));
            Assert.Throws<ArgumentException>(() =>
                ByteUtils.DeserializeUpToInt32BigEndian(new byte[2], 0, -1, false));
            Assert.Throws<ArgumentException>(() =>
                ByteUtils.DeserializeUpToInt32BigEndian(new byte[20], 0, 10, true));
        }

        [Theory]
        [MemberData(nameof(CreateTestParseInt48Data))]
        public void TestParseInt48(string input, long expected)
        {
            var actual = ByteUtils.ParseInt48(input);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestParseInt48Data()
        {
            return new List<object[]>
            {
                new object[]{ "0", 0 },
                new object[]{ "1", 1 },
                new object[]{ "2", 2 },
                new object[]{ " 20", 20 },
                new object[]{ " 200 ", 200 },
                new object[]{ "-1000", -1000 },
                new object[]{ "1000000", 1_000_000 },
                new object[]{ "-1000000000", -1_000_000_000 },
                new object[]{ "4294967295", 4_294_967_295 },
                new object[]{ "-50000000000000", -50_000_000_000_000 },
                new object[]{ "100000000000000", 100_000_000_000_000 },
                new object[]{ "140737488355327", 140_737_488_355_327 },
                new object[]{ "-140737488355328", -140_737_488_355_328 }
            };
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("false")]
        [InlineData("xyz")]
        [InlineData("1.23")]
        [InlineData("2.0")]
        [InlineData("140737488355328")]
        [InlineData("-140737488355329")]
        [InlineData("72057594037927935")]
        public void TestParsetInt48ForErrors(string input)
        {
            var ex = Assert.ThrowsAny<Exception>(() =>
                ByteUtils.ParseInt48(input));
            if (input != null)
            {
                Assert.IsAssignableFrom<FormatException>(ex);
            }
        }
    }
}
