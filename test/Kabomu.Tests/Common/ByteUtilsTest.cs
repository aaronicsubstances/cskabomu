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
        [MemberData(nameof(CreateTestSerializeUpToInt64BigEndianData))]
        public void TestSerializeUpToInt64BigEndian(long v, byte[] rawBytes,
            int offset, int length, byte[] expected)
        {
            byte[] actual = new byte[rawBytes.Length];
            Array.Copy(rawBytes, actual, rawBytes.Length);
            ByteUtils.SerializeUpToInt64BigEndian(v, actual, offset, length);
            Assert.Equal(actual, expected);
        }

        public static List<object[]> CreateTestSerializeUpToInt64BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ 35536, new byte[] { 8, 2 }, 0, 2, new byte[] { 138, 208 } },
                new object[]{ 65535, new byte[] { 0, 1, 2 }, 0, 2, new byte[] { 255, 255, 2 } },
                new object[]{ 2_294_967_196, new byte[] { 8, 2, 1, 0, 0, 0, 1 }, 
                    2, 4, new byte[] { 8, 2, 0x88, 0xca, 0x6b, 0x9c, 1 } },
                new object[]{ 4_294_967_295, new byte[] { 8, 2, 1, 0, 0, 0, 1 },
                    2, 4, new byte[] { 8, 2, 0xff, 0xff, 0xff, 0xff, 1 } },
                new object[]{ 72_057_594_037_927_935, new byte[] { 8, 2, 1, 0, 0, 0, 1 },
                    0, 7, new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff } }
            };
        }

        [Fact]
        public void TestSerializeUpToInt64BigEndianForErrors()
        {
            Assert.Throws<ArgumentException>(() => ByteUtils.SerializeUpToInt64BigEndian(1, new byte[2], -1, 0));
            Assert.Throws<ArgumentException>(() => ByteUtils.SerializeUpToInt64BigEndian(2, new byte[2], 0, -1));
            Assert.Throws<ArgumentException>(() => ByteUtils.SerializeUpToInt64BigEndian(3, new byte[20], 0, 10));
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeUpToInt64BigEndianData))]
        public void TestDeserializeUpToInt64BigEndian(byte[] rawBytes,
            int offset, int length, bool signed, long expected)
        {
            var actual = ByteUtils.DeserializeUpToInt64BigEndian(rawBytes, offset, length, signed);
            Assert.Equal(actual, expected);
        }

        public static List<object[]> CreateTestDeserializeUpToInt64BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 138, 208 }, 0, 2, false, 35536 },
                new object[]{ new byte[] { 255, 255, 2 }, 0, 2, false, 65535 },
                new object[]{ new byte[] { 255, 255, 2 }, 0, 2, true, -1 },
                new object[]{ new byte[] { 8, 2, 0x88, 0xca, 0x6b, 0x9c, 1 }, 2, 4, false, 2_294_967_196 },
                new object[]{ new byte[] { 8, 2, 0xff, 0xff, 0xff, 0xff, 1 }, 2, 4, false, 4_294_967_295 },
                new object[]{ new byte[] { 8, 2, 0xff, 0xff, 0xff, 0xff, 1 }, 2, 4, true, -1 },
                new object[]{ new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, 0, 7, false, 72_057_594_037_927_935 },
                new object[]{ new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, 0, 7, true, -1 }
            };
        }

        [Fact]
        public void TestDeserializeUpToInt64BigEndianForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
                ByteUtils.DeserializeUpToInt64BigEndian(new byte[2], -1, 0, false));
            Assert.Throws<ArgumentException>(() =>
                ByteUtils.DeserializeUpToInt64BigEndian(new byte[2], 0, -1, false));
            Assert.Throws<ArgumentException>(() =>
                ByteUtils.DeserializeUpToInt64BigEndian(new byte[20], 0, 10, true));
        }

        [Theory]
        [MemberData(nameof(CreateTestCalculateSizeOfSlicesData))]
        public void TestCalculateSizeOfSlices(ByteBufferSlice[] slices, int expected)
        {
            var actual = ByteUtils.CalculateSizeOfSlices(slices);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestCalculateSizeOfSlicesData()
        {
            var testData = new List<object[]>();

            var slices = new ByteBufferSlice[]
            {
                new ByteBufferSlice
                {
                    Data = new byte[5],
                    Length = 5
                },
                new ByteBufferSlice
                {
                    Data = new byte[4],
                    Length = 2
                },
                new ByteBufferSlice
                {
                    Data = new byte[3],
                    Length = 0
                },
                new ByteBufferSlice
                {
                    Data = new byte[1],
                    Length = 1
                }
            };
            int expected = 8;
            testData.Add(new object[] { slices, expected });

            slices = new ByteBufferSlice[]
            {
                new ByteBufferSlice
                {
                    Data = new byte[4],
                    Length = 2
                }
            };
            expected = 2;
            testData.Add(new object[] { slices, expected });

            slices = new ByteBufferSlice[]
            {
                new ByteBufferSlice
                {
                    Data = new byte[4],
                    Length = 0
                }
            };
            expected = 0;
            testData.Add(new object[] { slices, expected });

            slices = new ByteBufferSlice[0];
            expected = 0;
            testData.Add(new object[] { slices, expected });

            return testData;
        }

        [Fact]
        public void TestCalculateSizeOfSlicesForErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var slices = new ByteBufferSlice[]
                {
                    new ByteBufferSlice(),
                    null
                };
                ByteUtils.CalculateSizeOfSlices(slices);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                var slices = new ByteBufferSlice[]
                {
                    new ByteBufferSlice
                    {
                        Length = -1
                    }
                };
                ByteUtils.CalculateSizeOfSlices(slices);
            });
        }
    }
}
