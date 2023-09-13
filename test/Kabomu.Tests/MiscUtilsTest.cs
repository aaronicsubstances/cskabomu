using Kabomu.Abstractions;
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

namespace Kabomu.Tests
{
    public class MiscUtilsTest
    {
        /*[Fact]
        public async Task TestReadBytesFully()
        {
            // arrange
            var reader = ComparisonUtils.CreateRandomizedChunkStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[6];

            // act
            await MiscUtils.ReadExactBytesAsync(reader, readBuffer, 0, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 0, 1, 2 }, 0, 3,
                readBuffer, 0, 3);

            // assert that zero length reading doesn't cause problems.
            await MiscUtils.ReadExactBytesAsync(reader, readBuffer, 3, 0);

            // act again
            await MiscUtils.ReadExactBytesAsync(reader, readBuffer, 1, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 3, 4, 5 }, 0, 3,
                readBuffer, 1, 3);

            // act again
            await MiscUtils.ReadExactBytesAsync(reader, readBuffer, 3, 2);

            // assert
            ComparisonUtils.CompareData(new byte[] { 6, 7 }, 0, 2,
                readBuffer, 3, 2);

            // test zero byte reads.
            readBuffer = new byte[] { 2, 3, 5, 8 };
            await MiscUtils.ReadExactBytesAsync(reader, readBuffer, 0, 0);
            Assert.Equal(new byte[] { 2, 3, 5, 8 }, readBuffer);
        }

        [Fact]
        public async Task TestReadBytesFullyForErrors()
        {
            // arrange
            var reader = ComparisonUtils.CreateRandomizedChunkStream(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[5];

            // act
            await MiscUtils.ReadExactBytesAsync(reader, readBuffer, 0, readBuffer.Length);

            // assert
            ComparisonUtils.CompareData(
                new byte[] { 0, 1, 2, 3, 4 }, 0, readBuffer.Length,
                readBuffer, 0, readBuffer.Length);

            // act and assert unexpected end of read
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
                MiscUtils.ReadExactBytesAsync(reader, readBuffer, 0, readBuffer.Length));
            Assert.Contains("end of read", actualEx.Message);
        }

        [Theory]
        [MemberData(nameof(CreateTestReadAllBytesData))]
        public async Task TestReadAllBytes(int bufferingLimit, byte[] expected)
        {
            // arrange
            var reader = ComparisonUtils.CreateRandomizedChunkStream(expected);

            // act
            var actual = (await MiscUtils.ReadAllBytes(reader, bufferingLimit)).ToArray();

            // assert
            Assert.Equal(expected, actual);

            // assert that reader has been exhausted.
            actual = (await MiscUtils.ReadAllBytes(reader)).ToArray();
            Assert.Empty(actual);
        }

        public static List<object[]> CreateTestReadAllBytesData()
        {
            var testData = new List<object[]>();

            int bufferingLimit = 0;
            byte[] expected = new byte[0];
            testData.Add(new object[] { bufferingLimit, expected });

            bufferingLimit = 0;
            expected = new byte[] { 2 };
            testData.Add(new object[] { bufferingLimit, expected });

            bufferingLimit = 6;
            expected = new byte[] { 0, 1, 2, 5, 6, 7 };
            testData.Add(new object[] { bufferingLimit, expected });

            bufferingLimit = 6;
            expected = new byte[] { 0, 1, 4, 5, 6, 7 };
            testData.Add(new object[] { bufferingLimit, expected });

            bufferingLimit = 10;
            expected = new byte[] { 0, 1, 2, 4, 5, 6, 7, 9 };
            testData.Add(new object[] { bufferingLimit, expected });

            bufferingLimit = -1;
            expected = new byte[] { 3, 0, 1, 2, 4, 5, 6, 7, 9, 8, 10, 11, 12,
                113, 114 };
            testData.Add(new object[] { bufferingLimit, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadAllBytesForErrorsData))]
        public async Task TestReadAllBytesForErrors(byte[] srcData, int bufferingLimit)
        {
            // arrange
            var reader = ComparisonUtils.CreateRandomizedChunkStream(srcData);

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
                MiscUtils.ReadAllBytes(reader, bufferingLimit));

            // assert
            Assert.Contains($"limit of {bufferingLimit}", actualEx.Message);
        }

        public static List<object[]> CreateTestReadAllBytesForErrorsData()
        {
            var testData = new List<object[]>();

            byte[] srcData = new byte[] { 0, 1, 2, 5, 6, 7 };
            int bufferingLimit = 5;
            testData.Add(new object[] { srcData, bufferingLimit });

            srcData = new byte[] { 0, 1, 2, 4, 5, 6, 7, 9 };
            bufferingLimit = 7;
            testData.Add(new object[] { srcData, bufferingLimit });

            srcData = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 9 };
            bufferingLimit = 8;
            testData.Add(new object[] { srcData, bufferingLimit });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestCopyBytesData))]
        public async Task TestCopyBytesToStream(string srcData)
        {
            // arrange
            var expected = MiscUtils.StringToBytes(srcData);
            var readerStream = new MemoryStream(expected);
            var writerStream = new MemoryStream();

            // act
            await MiscUtils.CopyBytesToStream(readerStream, writerStream);

            // assert
            Assert.Equal(expected, writerStream.ToArray());
        }

        [MemberData(nameof(CreateTestCopyBytesData))]
        [Theory]
        public async Task TestCopyBytesToSinkWithRemainingBytes(string srcData)
        {
            // arrange
            var expected = MiscUtils.StringToBytes(srcData);

            // double the expectation and read half way,
            // to test that remaining bytes are correctly copied
            var reader = ComparisonUtils.CreateRandomizedChunkStream(
                MiscUtils.StringToBytes(srcData + srcData));
            var actual = new byte[expected.Length];
            await MiscUtils.ReadExactBytesAsync(reader, actual, 0, actual.Length);
            Assert.Equal(expected, actual);

            // now continue to test copyBytes() on
            // remaining data
            var writerStream = new MemoryStream();
            Func<byte[], int, int, Task> writerStreamWrapper = (data, offset, length) =>
                writerStream.WriteAsync(data, offset, length);

            // act
            await MiscUtils.CopyBytesToSink(reader, writerStreamWrapper);

            // assert
            actual = writerStream.ToArray();
            Assert.Equal(expected, actual);

            // assert that reader has been exhausted.
            actual = (await MiscUtils.ReadAllBytes(reader)).ToArray();
            Assert.Empty(actual);
        }

        public static List<object[]> CreateTestCopyBytesData()
        {
            return new List<object[]>
            {
                new object[]{ "" },
                new object[]{ "ab" },
                new object[]{ "abc" },
                new object[]{ "abcd" },
                new object[]{ "abcde" },
                new object[]{ "abcdef" }
            };
        }

        [Fact]
        public async Task TestCopyBytesWithEmptyReaderAndProblematicWriter()
        {
            var reader = new MemoryStream();
            Func<byte[], int, int, Task> writer = (data, offset, length) =>
                throw new Exception("broken!");
            await MiscUtils.CopyBytesToSink(reader, writer);
        }

        [Fact]
        public async Task TestCopyBytesForErrors1()
        {
            var reader = new MemoryStream(new byte[17]);
            Func<byte[], int, int, Task> writer = (data, offset, length) =>
                throw new Exception("broken!");

            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                MiscUtils.CopyBytesToSink(reader, writer));
            Assert.Equal("broken!", actualEx.Message);
        }

        [Fact]
        public async Task TestCopyBytesForErrors2()
        {
            var firstReader = new MemoryStream(new byte[2000]);
            Func<byte[], int, int, Task<int>> readerWrapper = async (data, offset, length) =>
            {
                var result = firstReader.Read(data, offset, length);
                if (result > 0)
                {
                    return result;
                }
                throw new Exception("killed in action");
            };
            var readerStream = ComparisonUtils.CreateInputStreamFromSource(
                readerWrapper);
            Func<byte[], int, int, Task> writer = (data, offset, length) =>
                Task.CompletedTask;

            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                MiscUtils.CopyBytesToSink(readerStream, writer));
            Assert.Equal("killed in action", actualEx.Message);
        }

        [Fact]
        public async Task TestCopyBytesForErrors3()
        {
            var firstReader = new MemoryStream(new byte[2000]);
            Func<byte[], int, int, Task<int>> readerWrapper = async (data, offset, length) =>
            {
                throw new Exception("killed in action");
            };
            var readerStream = ComparisonUtils.CreateInputStreamFromSource(
                readerWrapper);
            Func<byte[], int, int, Task> writer = (data, offset, length) =>
            {
                throw new Exception("broken"!);
            };

            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                MiscUtils.CopyBytesToSink(readerStream, writer));
            Assert.Equal("killed in action", actualEx.Message);
        }*/

        [Theory]
        [MemberData(nameof(CreateTestParseInt48Data))]
        public void TestParseInt48(string input, long expected)
        {
            var actual = MiscUtils.ParseInt48(input);
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
                MiscUtils.ParseInt48(input));
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
            var actual = MiscUtils.ParseInt32(input);
            Assert.Equal(expected, actual);

            input = "172";
            expected = 172;
            actual = MiscUtils.ParseInt32(input);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestParseInt32ForErrors()
        {
            Assert.Throws<FormatException>(() =>
                MiscUtils.ParseInt32(""));

            Assert.Throws<FormatException>(() =>
                MiscUtils.ParseInt32("x"));
        }

        [Theory]
        [MemberData(nameof(CreateTestIsValidByteBufferSliceData))]
        public void TestIsValidByteBufferSlice(byte[] data, int offset, int length, bool expected)
        {
            bool actual = MiscUtils.IsValidByteBufferSlice(data, offset, length);
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
            var actual = MiscUtils.StringToBytes("");
            Assert.Equal(new byte[0], actual);

            actual = MiscUtils.StringToBytes("abc");
            Assert.Equal(new byte[] { (byte)'a', (byte)'b', (byte)'c' }, actual);

            actual = MiscUtils.StringToBytes("Foo \u00a9 bar \U0001d306 baz \u2603 qux");
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
            var actual = MiscUtils.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
            actual = MiscUtils.BytesToString(data);
            Assert.Equal(expected, actual);

            offset = 0;
            data = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
            length = data.Length;
            expected = "abc";
            actual = MiscUtils.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
            actual = MiscUtils.BytesToString(data);
            Assert.Equal(expected, actual);

            offset = 1;
            data = new byte[] { 0x46, 0x6f, 0x6f, 0x20, 0xc2, 0xa9, 0x20, 0x62, 0x61, 0x72, 0x20,
                0xf0, 0x9d, 0x8c, 0x86, 0x20, 0x62, 0x61, 0x7a, 0x20, 0xe2, 0x98, 0x83,
                0x20, 0x71, 0x75, 0x78 };
            length = data.Length - 2;
            expected = "oo \u00a9 bar \U0001d306 baz \u2603 qu";
            actual = MiscUtils.BytesToString(data, offset, length);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestGetByteCount()
        {
            var actual = MiscUtils.GetByteCount("");
            Assert.Equal(0, actual);

            actual = MiscUtils.GetByteCount("abc");
            Assert.Equal(3, actual);

            actual = MiscUtils.GetByteCount("Foo \u00a9 bar \U0001d306 baz \u2603 qux");
            Assert.Equal(27, actual);
        }

        [Fact]
        public void TestConcatBuffers()
        {
            var chunks = new List<byte[]>();
            var actual = MiscUtils.ConcatBuffers(chunks);
            Assert.Empty(actual);

            chunks.Add(new byte[] { 108 });
            actual = MiscUtils.ConcatBuffers(chunks);
            Assert.Equal(new byte[] { 108 }, actual);

            chunks.Add(new byte[] { 4, 108, 2 });
            actual = MiscUtils.ConcatBuffers(chunks);
            Assert.Equal(new byte[] { 108, 4, 108, 2 }, actual);
        }

        [Fact]
        public void TestMergeProcessingOptions1()
        {
            IQuasiHttpProcessingOptions preferred = null;
            IQuasiHttpProcessingOptions fallback = null;
            var actual = MiscUtils.MergeProcessingOptions(
                preferred, fallback);
            Assert.Null(actual);
        }


        [Fact]
        public void TestMergeProcessingOptions2()
        {
            var preferred = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "tht" }
                },
                MaxHeadersSize = 10,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = -1,
                TimeoutMillis = 0
            };
            var fallback = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "htt" },
                    { "two", 2 }
                },
                MaxHeadersSize = 30,
                ResponseBufferingEnabled = true,
                ResponseBodyBufferingSizeLimit = 40,
                TimeoutMillis = -1
            };
            var actual = MiscUtils.MergeProcessingOptions(
                preferred, fallback);
            var expected = new DefaultQuasiHttpProcessingOptions
            {
                ExtraConnectivityParams = new Dictionary<string, object>
                {
                    { "scheme", "tht" },
                    { "two", 2 }
                },
                MaxHeadersSize = 10,
                ResponseBufferingEnabled = false,
                ResponseBodyBufferingSizeLimit = 40,
                TimeoutMillis = -1
            };
            ComparisonUtils.CompareProcessingOptions(expected, actual);
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveNonZeroIntegerOptionData))]
        public void TestDetermineEffectiveNonZeroIntegerOption(int? preferred,
            int? fallback1, int defaultValue, int expected)
        {
            var actual = MiscUtils.DetermineEffectiveNonZeroIntegerOption(
                preferred, fallback1, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveNonZeroIntegerOptionData()
        {
            var testData = new List<object[]>();

            int? preferred = 1;
            int? fallback1 = null;
            int defaultValue = 20;
            int expected = 1;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = 5;
            fallback1 = 3;
            defaultValue = 11;
            expected = 5;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = -15;
            fallback1 = 3;
            defaultValue = -1;
            expected = -15;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = 3;
            defaultValue = -1;
            expected = 3;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = -3;
            defaultValue = -1;
            expected = -3;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 2;
            expected = 2;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = -8;
            expected = -8;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 0;
            expected = 0;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectivePositiveIntegerOptionnData))]
        public void TestDetermineEffectivePositiveIntegerOption(int? preferred, int? fallback1,
            int defaultValue, int expected)
        {
            var actual = MiscUtils.DetermineEffectivePositiveIntegerOption(preferred, fallback1,
                defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectivePositiveIntegerOptionnData()
        {
            var testData = new List<object[]>();

            int? preferred = null;
            int? fallback1 = 1;
            int defaultValue = 30;
            int expected = 1;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = 5;
            fallback1 = 3;
            defaultValue = 11;
            expected = 5;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = 3;
            defaultValue = -1;
            expected = 3;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 2;
            expected = 2;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = -8;
            expected = -8;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = 0;
            expected = 0;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveOptionsData))]
        public void TestDetermineEffectiveOptions(IDictionary<string, object> preferred,
            IDictionary<string, object> fallback, IDictionary<string, object> expected)
        {
            var actual = MiscUtils.DetermineEffectiveOptions(preferred, fallback);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveOptionsData()
        {
            var testData = new List<object[]>();

            IDictionary<string, object> preferred = null;
            IDictionary<string, object> fallback = null;
            var expected = new Dictionary<string, object>();
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>();
            fallback = new Dictionary<string, object>();
            expected = new Dictionary<string, object>();
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            fallback = null;
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = null;
            fallback = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            fallback = new Dictionary<string, object>
            {
                { "c", 4 }, { "d", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 },
                { "c", 4 }, { "d", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }
            };
            fallback = new Dictionary<string, object>
            {
                { "a", 4 }, { "d", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "b", 3 }, { "d", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            preferred = new Dictionary<string, object>
            {
                { "a", 2 }
            };
            fallback = new Dictionary<string, object>
            {
                { "a", 4 }, { "d", 3 }
            };
            expected = new Dictionary<string, object>
            {
                { "a", 2 }, { "d", 3 }
            };
            testData.Add(new object[] { preferred, fallback, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDetermineEffectiveBooleanOptionData))]
        public void TestDetermineEffectiveBooleanOption(bool? preferred,
            bool? fallback1, bool defaultValue, bool expected)
        {
            var actual = MiscUtils.DetermineEffectiveBooleanOption(
                preferred, fallback1, defaultValue);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDetermineEffectiveBooleanOptionData()
        {
            var testData = new List<object[]>();

            bool? preferred = true;
            bool? fallback1 = null;
            bool defaultValue = true;
            bool expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = false;
            fallback1 = true;
            defaultValue = true;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = false;
            defaultValue = true;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = true;
            defaultValue = false;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = true;
            defaultValue = true;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = true;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = null;
            fallback1 = null;
            defaultValue = false;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = true;
            fallback1 = true;
            defaultValue = false;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = true;
            fallback1 = true;
            defaultValue = true;
            expected = true;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            preferred = false;
            fallback1 = false;
            defaultValue = false;
            expected = false;
            testData.Add(new object[] { preferred, fallback1, defaultValue,
                expected });

            return testData;
        }

        [Fact]
        public void TestCreateCancellableTimeoutTask1()
        {
            var actual = MiscUtils.CreateCancellableTimeoutTask(0);
            Assert.Null(actual);
        }

        [Fact]
        public void TestCreateCancellableTimeoutTask2()
        {
            var actual = MiscUtils.CreateCancellableTimeoutTask(-3);
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestCreateCancellableTimeoutTask3()
        {
            var p = MiscUtils.CreateCancellableTimeoutTask(50);
            Assert.NotNull(p.Task);
            var result = await p.Task;
            Assert.True(result);
            p.Cancel();
            result = await p.Task;
            Assert.True(result);
        }

        [Fact]
        public async Task TestCreateCancellableTimeoutTask4()
        {
            var p = MiscUtils.CreateCancellableTimeoutTask(500);
            Assert.NotNull(p.Task);
            await Task.Delay(100);
            p.Cancel();
            var result = await p.Task;
            Assert.False(result);
            p.Cancel();
            result = await p.Task;
            Assert.False(result);
        }

        [Fact]
        public async Task TestWhenAnyFailOrAllSucceed1()
        {
            var tasks = new List<Task>();
            await MiscUtils.WhenAnyFailOrAllSucceed(tasks);
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
                MiscUtils.WhenAnyFailOrAllSucceed(tasks));
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
                MiscUtils.WhenAnyFailOrAllSucceed(tasks));
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
                MiscUtils.WhenAnyFailOrAllSucceed(tasks));
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
            await MiscUtils.WhenAnyFailOrAllSucceed(tasks);
        }
    }
}
