﻿using Kabomu.Common;
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
        [MemberData(nameof(CreateTestReadBytesFullyData))]
        public async Task TestReadBytesFully(
            IQuasiHttpTransport transport, object connection,
            byte[] data, int offset, int bytesToRead,
            string expectedError, string expectedData)
        {
            var tcs = new TaskCompletionSource<int>();
            ByteUtils.ReadBytesFully(transport, connection, data, offset, bytesToRead,
                e =>
                {
                    if (e != null)
                    {
                        tcs.SetException(e);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                });
            Exception actualException = null;
            try
            {
                await tcs.Task;
            }
            catch (Exception e)
            {
                actualException = e;
            }
            if (expectedError != null)
            {
                Assert.NotNull(actualException);
                Assert.Equal(expectedError, actualException.Message);
            }
            else
            {
                Assert.Null(actualException);
                string actualData = Encoding.UTF8.GetString(data, offset, bytesToRead);
                Assert.Equal(expectedData, actualData);
            }
        }

        public static List<object[]> CreateTestReadBytesFullyData()
        {
            var testData = new List<object[]>();

            object connection = "tea";
            var transport = new NullTransport(connection, new string[] { "car", "e" }, null, 0);
            byte[] data = new byte[4];
            int offset = 0;
            int bytesToRead = data.Length;
            string expectedError = null;
            string expectedData = "care";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = null;
            transport = new NullTransport(connection, new string[] { "are" }, null, 0);
            data = new byte[4];
            offset = 1;
            bytesToRead = 3;
            expectedError = null;
            expectedData = "are";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new NullTransport(connection, new string[] { "sen", "der", "s" }, null, 0);
            data = new byte[10];
            offset = 2;
            bytesToRead = 7;
            expectedError = null;
            expectedData = "senders";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new NullTransport(connection, new string[] { "123", "der", "." }, null, 0);
            data = new byte[10];
            offset = 2;
            bytesToRead = 8;
            expectedError = "end of read";
            expectedData = null;
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new NullTransport(connection, new string[0], null, 0);
            data = new byte[10];
            offset = 7;
            bytesToRead = 0;
            expectedError = null;
            expectedData = "";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestTransferBodyToTransportData))]
        public async Task TestTransferBodyToTransport(object connection, string bodyData,
            int chunkSize, int maxWriteCount, string expectedError)
        {
            var tcs = new TaskCompletionSource<int>();
            var savedWrites = new StringBuilder();
            var transport = new NullTransport(connection, null, savedWrites, maxWriteCount);
            transport.MaxMessageOrChunkSize = chunkSize;
            var bodyBytes = Encoding.UTF8.GetBytes(bodyData);
            var body = new ByteBufferBody(bodyBytes, 0, bodyBytes.Length, null);
            ByteUtils.TransferBodyToTransport(transport, connection, body, new TestEventLoopApi(),
                e =>
                {
                    if (e != null)
                    {
                        tcs.SetException(e);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                });
            Exception actualException = null;
            try
            {
                await tcs.Task;
            }
            catch (Exception e)
            {
                actualException = e;
            }
            if (expectedError != null)
            {
                Assert.NotNull(actualException);
                Assert.Equal(expectedError, actualException.Message);
            }
            else
            {
                Assert.Null(actualException);
                Assert.Equal(bodyData, savedWrites.ToString());
            }
        }

        public static List<object[]> CreateTestTransferBodyToTransportData()
        {
            var testData = new List<object[]>();

            object connection = "tea";
            string bodyData = "care";
            int chunkSize = 1;
            int maxWriteCount = 4;
            string expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "";
            chunkSize = 0;
            maxWriteCount = 0;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = 4;
            bodyData = "tintontannn!!!";
            chunkSize = 10;
            maxWriteCount = 2;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "!!!";
            chunkSize = 2;
            maxWriteCount = 1;
            expectedError = "END";
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestIsValidMessagePayloadData))]
        public void TestIsValidMessagePayload(byte[] data, int offset, int length, bool expected)
        {
            bool actual = ByteUtils.IsValidMessagePayload(data, offset, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestIsValidMessagePayloadData()
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

            offset = 0;
            data = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            length = data.Length;
            expected = "abc";
            actual = ByteUtils.BytesToString(data, offset, length);
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
        [MemberData(nameof(CreateConvertBytesToHexData))]
        public void TestConvertBytesToHex(byte[] data, int offset, int length, string expected)
        {
            string actual = ByteUtils.ConvertBytesToHex(data, offset, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateConvertBytesToHexData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { }, 0, 0, "" },
                new object[]{ new byte[] { 0xFF }, 0, 1, "ff" },
                new object[]{ new byte[] { 0, 0x68, 0x65, 0x6c }, 0, 4,
                    "0068656c" },
                new object[]{ new byte[] { 0x01, 0x68, 0x65, 0x6c }, 0, 4,
                    "0168656c" },
                new object[]{ new byte[] { 0, 0x68, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64, 0 }, 1, 11,
                    "68656c6c6f20776f726c64" },
            };
        }

        [Theory]
        [MemberData(nameof(CreateConvertHexToBytesData))]
        public void TestConvertHexToBytes(string hex, byte[] expected)
        {
            byte[] actual = ByteUtils.ConvertHexToBytes(hex);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateConvertHexToBytesData()
        {
            return new List<object[]>
            {
                new object[]{ "", new byte[] { } },
                new object[]{ "ff", new byte[] { 0xFF } },
                new object[]{ "FF", new byte[] { 0xFF } },
                new object[]{ "0068656c", new byte[] { 0, 0x68, 0x65, 0x6c } },
                new object[]{ "68656C6c6F20776F726C64", new byte[] { 0x68, 0x65, 0x6c, 0x6c, 0x6f,
                    0x20, 0x77, 0x6f, 0x72, 0x6c, 0x64 } },
            };
        }

        [Fact]
        public void TestConvertHexToBytesForError()
        {
            Assert.ThrowsAny<Exception>(() => ByteUtils.ConvertHexToBytes("d"));
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

        [Theory]
        [MemberData(nameof(CreateTestSerializeInt16BigEndianData))]
        public void TestSerializeInt16BigEndian(short v, byte[] expected)
        {
            byte[] actual = ByteUtils.SerializeInt16BigEndian(v);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestSerializeInt16BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ 0, new byte[] { 0, 0 } },
                new object[]{ 1_000, new byte[] { 3, 232 } },
                new object[]{ 10_000, new byte[] { 39, 16 } },
                new object[]{ 30_000, new byte[] { 117, 48 } },
                new object[]{ -30_000, new byte[] { 138, 208 } },
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestSerializeInt32BigEndianData))]
        public void TestSerializeInt32BigEndian(int v, byte[] expected)
        {
            byte[] actual = ByteUtils.SerializeInt32BigEndian(v);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestSerializeInt32BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ 0, new byte[] { 0, 0, 0, 0 } },
                new object[]{ 1_000, new byte[] { 0, 0, 3, 232 } },
                new object[]{ 10_000, new byte[] { 0, 0, 39, 16 } },
                new object[]{ 30_000, new byte[] { 0, 0, 117, 48 } },
                new object[]{ -30_000, new byte[] { 255, 255, 138, 208 } },
                new object[]{ 1_000_000, new byte[] { 0, 15, 66, 64 } },
                new object[]{ 1_000_000_000, new byte[] { 59, 154, 202, 0 } },
                new object[]{ 2_000_000_100, new byte[] { 119, 53, 148, 100 } },
                new object[]{ -2_000_000_100, new byte[] { 136, 202, 107, 156 } },
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestSerializeInt64BigEndianData))]
        public void TestSerializeInt64BigEndian(long v, byte[] expected)
        {
            byte[] actual = ByteUtils.SerializeInt64BigEndian(v);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestSerializeInt64BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ 0, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 } },
                new object[]{ 1_000, new byte[] { 0, 0, 0, 0, 0, 0, 3, 232 } },
                new object[]{ 10_000, new byte[] { 0, 0, 0, 0, 0, 0, 39, 16 } },
                new object[]{ 30_000, new byte[] { 0, 0, 0, 0, 0, 0, 117, 48 } },
                new object[]{ -30_000, new byte[] { 255, 255, 255, 255, 255, 255, 138, 208 } },
                new object[]{ 1_000_000, new byte[] { 0, 0, 0, 0, 0, 15, 66, 64 } },
                new object[]{ 1_000_000_000, new byte[] { 0, 0, 0, 0, 59, 154, 202, 0 } },
                new object[]{ 2_000_000_100, new byte[] { 0, 0, 0, 0, 119, 53, 148, 100 } },
                new object[]{ -2_000_000_100, new byte[] { 255, 255, 255, 255, 136, 202, 107, 156 } },
                new object[]{ 1_000_000_000_000L, new byte[] { 0, 0, 0, 232, 212, 165, 16, 0 } },
                new object[]{ 1_000_000_000_000_000L, new byte[] { 0, 3, 141, 126, 164, 198, 128, 0 } },
                new object[]{ 1_000_000_000_000_000_000L, new byte[] { 13, 224, 182, 179, 167, 100, 0, 0 } },
                new object[]{ 2_000_000_000_000_000_000L, new byte[] { 27, 193, 109, 103, 78, 200, 0, 0 } },
                new object[]{ 4_000_000_000_000_000_000L, new byte[] { 55, 130, 218, 206, 157, 144, 0, 0 } },
                new object[]{ 9_000_000_000_000_000_000L, new byte[] { 124, 230, 108, 80, 226, 132, 0, 0 } },
                new object[]{ 9_199_999_999_999_999_999L, new byte[] { 127, 172, 247, 65, 157, 151, 255, 255 } },
                new object[]{ -9_199_999_999_999_999_999L, new byte[] { 128, 83, 8, 190, 98, 104, 0, 1 } }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeUpToInt64BigEndianData))]
        public void TestDeserializeUpToInt64BigEndian(byte[] rawBytes,
            int offset, int length, long expected)
        {
            var actual = ByteUtils.DeserializeUpToInt64BigEndian(rawBytes, offset, length);
            Assert.Equal(actual, expected);
        }

        public static List<object[]> CreateTestDeserializeUpToInt64BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 138, 208 }, 0, 2, 35536 },
                new object[]{ new byte[] { 255, 255, 2 }, 0, 2, 65535 },
                new object[]{ new byte[] { 8, 2, 0x88, 0xca, 0x6b, 0x9c, 1 }, 2, 4, 2_294_967_196 },
                new object[]{ new byte[] { 8, 2, 0xff, 0xff, 0xff, 0xff, 1 }, 2, 4, 4_294_967_295 },
                new object[]{ new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, 0, 7, 72_057_594_037_927_935 }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeInt16BigEndianData))]
        public void TestDeserializeInt16BigEndian(byte[] data, int offset, short expected)
        {
            short actual = ByteUtils.DeserializeInt16BigEndian(data, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDeserializeInt16BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 0, 0 }, 0, 0 },
                new object[]{ new byte[] { 3, 232 }, 0, 1000 },
                new object[]{ new byte[] { 39, 16, 1 }, 0, 10_000 },
                new object[]{ new byte[] { 0, 117, 48, 1 }, 1, 30_000 },
                new object[]{ new byte[] { 0, 138, 208 }, 1, -30_000 },
                new object[]{ new byte[] { 255, 255 }, 0, -1 }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeInt32BigEndianData))]
        public void TestDeserializeInt32BigEndian(byte[] data, int offset, int expected)
        {
            int actual = ByteUtils.DeserializeInt32BigEndian(data, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDeserializeInt32BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 0, 0, 0, 0 }, 0, 0 },
                new object[]{ new byte[] { 0, 0, 3, 232 }, 0, 1_000 },
                new object[]{ new byte[] { 0, 0, 39, 16 }, 0, 10_000 },
                new object[]{ new byte[] { 0, 0, 117, 48 }, 0, 30_000 },
                new object[]{ new byte[] { 255, 255, 138, 208 }, 0, -30_000 },
                new object[]{ new byte[] { 0, 15, 66, 64 }, 0, 1_000_000 },
                new object[]{ new byte[] { 59, 154, 202, 0, 1 }, 0, 1_000_000_000 },
                new object[]{ new byte[] { 0, 119, 53, 148, 100 }, 1, 2_000_000_100 },
                new object[]{ new byte[] { 0, 136, 202, 107, 156, 1 }, 1, -2_000_000_100 },
                new object[]{ new byte[] { 255, 255, 255, 255 }, 0, -1 }
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestDeserializeInt64BigEndianData))]
        public void TestDeserializeInt64BigEndian(byte[] data, int offset, long expected)
        {
            long actual = ByteUtils.DeserializeInt64BigEndian(data, offset);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDeserializeInt64BigEndianData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, 0 },
                new object[]{ new byte[] { 0, 0, 0, 0, 0, 0, 3, 232 }, 0, 1_000 },
                new object[]{ new byte[] { 0, 0, 0, 0, 0, 0, 39, 16 }, 0, 10_000 },
                new object[]{ new byte[] { 0, 0, 0, 0, 0, 0, 117, 48 }, 0, 30_000 },
                new object[]{ new byte[] { 255, 255, 255, 255, 255, 255, 138, 208 }, 0, -30_000 },
                new object[]{ new byte[] { 0, 0, 0, 0, 0, 15, 66, 64 }, 0, 1_000_000 },
                new object[]{ new byte[] { 0, 0, 0, 0, 59, 154, 202, 0 }, 0, 1_000_000_000 },
                new object[]{ new byte[] { 0, 0, 0, 0, 119, 53, 148, 100 }, 0, 2_000_000_100 },
                new object[]{ new byte[] { 255, 255, 255, 255, 136, 202, 107, 156 }, 0, -2_000_000_100 },
                new object[]{ new byte[] { 0, 0, 0, 232, 212, 165, 16, 0 }, 0, 1_000_000_000_000L },
                new object[]{ new byte[] { 0, 3, 141, 126, 164, 198, 128, 0 }, 0, 1_000_000_000_000_000L },
                new object[]{ new byte[] { 13, 224, 182, 179, 167, 100, 0, 0 }, 0, 1_000_000_000_000_000_000L },
                new object[]{ new byte[] { 27, 193, 109, 103, 78, 200, 0, 0 }, 0, 2_000_000_000_000_000_000L },
                new object[]{ new byte[] { 55, 130, 218, 206, 157, 144, 0, 0 }, 0, 4_000_000_000_000_000_000L },
                new object[]{ new byte[] { 124, 230, 108, 80, 226, 132, 0, 0, 1 }, 0, 9_000_000_000_000_000_000L },
                new object[]{ new byte[] { 0, 127, 172, 247, 65, 157, 151, 255, 255 }, 1, 9_199_999_999_999_999_999L },
                new object[]{ new byte[] { 0, 128, 83, 8, 190, 98, 104, 0, 1, 0 }, 1, -9_199_999_999_999_999_999L },
                new object[]{ new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, 0, -1 }
            };
        }
    }
}
