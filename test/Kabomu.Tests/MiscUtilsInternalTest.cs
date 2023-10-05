using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests
{
    public class MiscUtilsInternalTest
    {
        [Theory]
        [MemberData(nameof(CreateTestSerializeInt32BEData))]
        public void TestSerializeInt32BE(int v, byte[] rawBytes,
            int offset, byte[] expected)
        {
            byte[] actual = new byte[rawBytes.Length];
            Array.Copy(rawBytes, actual, rawBytes.Length);
            MiscUtilsInternal.SerializeInt32BE(v, actual, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestSerializeInt32BEData()
        {
            return new List<object[]>
            {
                new object[]{ 2001, new byte[] { 8, 2, 3, 4 }, 0, new byte[] { 0, 0, 7, 0xd1 } },
                new object[]{ -10_999, new byte[5], 1, new byte[] { 0, 0xff, 0xff, 0xd5, 9 } },
                new object[]{ 1_000_000, new byte[4], 0, new byte[] { 0, 0xf, 0x42, 0x40 } },
                new object[]{ 1_000_000_000, new byte[] { 10, 20, 30, 40, 50 }, 0, new byte[] { 0x3b, 0x9a, 0xca, 0, 50 } },
                new object[]{ -1_000_000_000, new byte[] { 10, 11, 12, 13,
                    10, 11, 12, 13 }, 2, new byte[] { 10, 11,
                    0xc4, 0x65, 0x36, 0, 12, 13 } }
            };
        }

        [Fact]
        public void TestSerializeInt32BEErrors()
        {
            Assert.ThrowsAny<Exception>(() =>
                MiscUtilsInternal.SerializeInt32BE(1, new byte[2], 0));
            Assert.ThrowsAny<Exception>(() =>
                MiscUtilsInternal.SerializeInt32BE(2, new byte[4], 1));
            Assert.ThrowsAny<Exception>(() =>
                MiscUtilsInternal.SerializeInt32BE(3, new byte[20], 18));
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeInt32BEData))]
        public void TestDeserializeInt32BE(byte[] rawBytes,
            int offset, int expected)
        {
            var actual = MiscUtilsInternal.DeserializeInt32BE(rawBytes, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDeserializeInt32BEData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 0, 0, 7, 0xd1 }, 0, 2001 },
                new object[]{ new byte[] { 0xff, 0xff, 0xd5, 9 }, 0, -10_999 },
                new object[]{ new byte[] { 0, 0xf, 0x42, 0x40 }, 0, 1_000_000 },
                new object[]{ new byte[] { 0x3b, 0x9a, 0xca, 0, 50 }, 0, 1_000_000_000 },
                new object[]{ new byte[] { 0xc4, 0x65, 0x36, 0 }, 0, -1_000_000_000 },
                // the next would have been 2_294_967_196 if deserializing entire 32-bits as unsigned.
                new object[]{ new byte[] { 8, 2, 0x88, 0xca, 0x6b, 0x9c, 1 }, 2, -2_000_000_100 }
            };
        }

        [Fact]
        public void TestDeserializeUpToInt32BigEndianForErrors()
        {
            Assert.ThrowsAny<Exception>(() =>
                MiscUtilsInternal.DeserializeInt32BE(new byte[2], 0));
            Assert.ThrowsAny<Exception>(() =>
                MiscUtilsInternal.DeserializeInt32BE(new byte[4], 1));
            Assert.ThrowsAny<Exception>(() =>
                MiscUtilsInternal.DeserializeInt32BE(new byte[20], 17));
        }

        [Theory]
        [MemberData(nameof(CreateTestParseInt48Data))]
        public void TestParseInt48(string input, long expected)
        {
            var actual = MiscUtilsInternal.ParseInt48(input);
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
                MiscUtilsInternal.ParseInt48(input));
            if (input != null)
            {
                Assert.IsAssignableFrom<FormatException>(ex);
            }
        }

        [Fact]
        public void TestParseInt32()
        {
            string input = " 67 ";
            int expected = 67;
            var actual = MiscUtilsInternal.ParseInt32(input);
            Assert.Equal(expected, actual);

            input = "172";
            expected = 172;
            actual = MiscUtilsInternal.ParseInt32(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestParseInt32ForErrors()
        {
            Assert.Throws<FormatException>(() =>
                MiscUtilsInternal.ParseInt32(""));

            Assert.Throws<FormatException>(() =>
                MiscUtilsInternal.ParseInt32("x"));
        }

        [Theory]
        [MemberData(nameof(CreateTestIsValidByteBufferSliceData))]
        public void TestIsValidByteBufferSlice(byte[] data, int offset, int length, bool expected)
        {
            bool actual = MiscUtilsInternal.IsValidByteBufferSlice(data, offset, length);
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
            var actual = MiscUtilsInternal.StringToBytes("");
            Assert.Equal(new byte[0], actual);

            actual = MiscUtilsInternal.StringToBytes("abc");
            Assert.Equal(new byte[] { (byte)'a', (byte)'b', (byte)'c' }, actual);

            actual = MiscUtilsInternal.StringToBytes("Foo \u00a9 bar \U0001d306 baz \u2603 qux");
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
            var actual = MiscUtilsInternal.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
            actual = MiscUtilsInternal.BytesToString(data);
            Assert.Equal(expected, actual);

            offset = 0;
            data = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            length = data.Length;
            expected = "abc";
            actual = MiscUtilsInternal.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
            actual = MiscUtilsInternal.BytesToString(data);
            Assert.Equal(expected, actual);

            offset = 1;
            data = new byte[] { 0x46, 0x6f, 0x6f, 0x20, 0xc2, 0xa9, 0x20, 0x62, 0x61, 0x72, 0x20,
                0xf0, 0x9d, 0x8c, 0x86, 0x20, 0x62, 0x61, 0x7a, 0x20, 0xe2, 0x98, 0x83,
                0x20, 0x71, 0x75, 0x78 };
            length = data.Length - 2;
            expected = "oo \u00a9 bar \U0001d306 baz \u2603 qu";
            actual = MiscUtilsInternal.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed1()
        {
            var tasks = new List<Task>();
            await MiscUtilsInternal.WhenAnyFailOrAllSucceed(tasks);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed3()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.FromException(new Exception("error3"))
            };
            var actualEx = await Assert.ThrowsAnyAsync<Exception>(() =>
                MiscUtilsInternal.WhenAnyFailOrAllSucceed(tasks));
            Assert.Equal("error3", actualEx.Message);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed4()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.FromException(new Exception("error4a")),
                Task.FromException(new Exception("error4b")),
                Task.CompletedTask
            };
            var actualEx = await Assert.ThrowsAnyAsync<Exception>(() =>
                MiscUtilsInternal.WhenAnyFailOrAllSucceed(tasks));
            Assert.Equal("error4a", actualEx.Message);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed5()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.FromException(new Exception("error5")),
                Task.CompletedTask
            };
            var actualEx = await Assert.ThrowsAnyAsync<Exception>(() =>
                MiscUtilsInternal.WhenAnyFailOrAllSucceed(tasks));
            Assert.Equal("error5", actualEx.Message);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed6()
        {
            var tasks = new List<Task>
            {
                Task.CompletedTask,
                Task.Delay(1000),
                Task.CompletedTask
            };
            await MiscUtilsInternal.WhenAnyFailOrAllSucceed(tasks);
        }
    }
}
