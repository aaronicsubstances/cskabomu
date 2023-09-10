using Kabomu.Abstractions;
using Kabomu.Exceptions;
using Kabomu.Impl;
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
        [Fact]
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
        }

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

        [Theory]
        [InlineData("", 3, "000")]
        [InlineData("1", 3, "001")]
        [InlineData("14", 3, "014")]
        [InlineData("614", 3, "614")]
        [InlineData("4a614", 5, "4a614")]
        [InlineData("4a614", 6, "04a614")]
        [InlineData("34a614", 6, "34a614")]
        public void TestPadLeftWithZeros(string v, int totalLen, string expected)
        {
            var actual = MiscUtils.PadLeftWithZeros(v, totalLen);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestCompleteMainTask1()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = null;
            var actual = await MiscUtils.CompleteMainTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteMainTask2()
        {
            Task workTask = Task.CompletedTask;
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = null;
            await MiscUtils.CompleteMainTask(
                workTask, timeoutTask, cancellationTask);
        }

        [Fact]
        public async Task TestCompleteMainTask3()
        {
            var expected = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.FromResult(
                expected as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            var actual = await MiscUtils.CompleteMainTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteMainTask4()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            Task<IQuasiHttpResponse> timeoutTask = null;
            Task<IQuasiHttpResponse> cancellationTask = Task.FromResult<IQuasiHttpResponse>(
                new DefaultQuasiHttpResponse());
            var actual = await MiscUtils.CompleteMainTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Null(actual);
        }

        [Fact]
        public async Task TestCompleteMainTask5()
        {
            DefaultQuasiHttpResponse expected = new DefaultQuasiHttpResponse(),
                instance2 = new DefaultQuasiHttpResponse(),
                instance3 = new DefaultQuasiHttpResponse();
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(0.75)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return expected;
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return instance2;
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return instance3;
                });
            var actual = await MiscUtils.CompleteMainTask(
                workTask, timeoutTask, cancellationTask);
            Assert.Same(expected, actual);
        }

        [Fact]
        public async Task TestCompleteMainTask6()
        {
            Task workTask = Task.Delay(TimeSpan.FromSeconds(2));
            Task timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error1");
                });
            Task cancellationTask = Task.Delay(TimeSpan.FromSeconds(1));
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.CompleteMainTask(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteMainTask7()
        {
            Task<IQuasiHttpResponse> workTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error1");
                });
            Task<IQuasiHttpResponse> timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error2");
                });
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error3");
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.CompleteMainTask(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error1", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteMainTask8()
        {
            Task workTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error1");
                });
            Task timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(2)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error2");
                });
            Task cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(0.5)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    throw new QuasiHttpRequestProcessingException("error3");
                });
            var actualEx = await Assert.ThrowsAsync<QuasiHttpRequestProcessingException>(() =>
            {
                return MiscUtils.CompleteMainTask(
                    workTask, timeoutTask, cancellationTask);
            });
            Assert.Equal("error3", actualEx.Message);
        }

        [Fact]
        public async Task TestCompleteMainTaskForErrors()
        {
            Task<IQuasiHttpResponse> workTask = null;
            Task<IQuasiHttpResponse> timeoutTask = Task.FromResult(
                new DefaultQuasiHttpResponse() as IQuasiHttpResponse);
            Task<IQuasiHttpResponse> cancellationTask = Task.Delay(
                TimeSpan.FromSeconds(1)).ContinueWith<IQuasiHttpResponse>(_ =>
                {
                    return null;
                });
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MiscUtils.CompleteMainTask(
                    workTask, timeoutTask, cancellationTask));
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
