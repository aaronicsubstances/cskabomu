using Kabomu.Common;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class IOUtilsTest
    {
        [Fact]
        public async Task TestReadBytes1()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var actual = new byte[3];
            var actualReadLen = await IOUtils.ReadBytes(stream, actual, 0, 2);
            Assert.Equal(2, actualReadLen);
            ComparisonUtils.CompareData(new byte[] { 1, 2 }, 0, 2,
                actual, 0, 2);
            actualReadLen = await IOUtils.ReadBytes(stream, actual, 1, 2);
            Assert.Equal(1, actualReadLen);
            ComparisonUtils.CompareData(new byte[] { 3 }, 0, 1,
                actual, 1, 1);
            actualReadLen = await IOUtils.ReadBytes(stream, actual, 2, 1);
            Assert.Equal(0, actualReadLen);
        }

        [Fact]
        public async Task TestReadBytes2()
        {
            var stream = new MemoryStream(new byte[] { 1, 2, 3 });
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    stream.ReadAsync(data, offset, length)
            };
            var actual = new byte[3];
            var actualReadLen = await IOUtils.ReadBytes(reader, actual, 0, 2);
            Assert.Equal(2, actualReadLen);
            ComparisonUtils.CompareData(new byte[] { 1, 2 }, 0, 2,
                actual, 0, 2);
            actualReadLen = await IOUtils.ReadBytes(reader, actual, 1, 2);
            Assert.Equal(1, actualReadLen);
            ComparisonUtils.CompareData(new byte[] { 3 }, 0, 1,
                actual, 1, 1);
            actualReadLen = await IOUtils.ReadBytes(reader, actual, 2, 1);
            Assert.Equal(0, actualReadLen);
        }

        [Fact]
        public async Task TestReadBytes3()
        {
            await Assert.ThrowsAnyAsync<Exception>(() =>
                IOUtils.ReadBytes(null, new byte[3], 0, 2));

            var falseReader = new object();
            await Assert.ThrowsAsync<InvalidCastException>(() =>
                IOUtils.ReadBytes(falseReader, new byte[3], 0, 2));
        }

        [Fact]
        public async Task TestWriteBytes1()
        {
            var stream = new MemoryStream();
            await IOUtils.WriteBytes(stream, new byte[] { 1, 2, 3 }, 0, 2);
            await IOUtils.WriteBytes(stream, new byte[] { 0, 3, 2, 1 }, 1, 2);
            Assert.Equal(new byte[] { 1, 2, 3, 2 }, stream.ToArray());
        }

        [Fact]
        public async Task TestWriteBytes2()
        {
            var stream = new MemoryStream();
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    stream.WriteAsync(data, offset, length)
            };
            await IOUtils.WriteBytes(writer, new byte[] { 1, 2, 3 }, 0, 2);
            await IOUtils.WriteBytes(writer, new byte[] { 0, 3, 2, 1 }, 1, 2);
            Assert.Equal(new byte[] { 1, 2, 3, 2 }, stream.ToArray());
        }

        [Fact]
        public async Task TestWriteBytes3()
        {
            await Assert.ThrowsAnyAsync<Exception>(() =>
                IOUtils.WriteBytes(null, new byte[3], 0, 2));

            var falseWriter = new object();
            await Assert.ThrowsAsync<InvalidCastException>(() =>
                IOUtils.WriteBytes(falseWriter, new byte[3], 0, 2));
        }

        [Fact]
        public async Task TestReadBytesFully()
        {
            // arrange
            var reader = new RandomizedReadSizeBufferReader(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[6];

            // act
            await IOUtils.ReadBytesFully(reader, readBuffer, 0, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 0, 1, 2 }, 0, 3,
                readBuffer, 0, 3);

            // assert that zero length reading doesn't cause problems.
            await IOUtils.ReadBytesFully(reader, readBuffer, 3, 0);

            // act again
            await IOUtils.ReadBytesFully(reader, readBuffer, 1, 3);

            // assert
            ComparisonUtils.CompareData(new byte[] { 3, 4, 5 }, 0, 3,
                readBuffer, 1, 3);

            // act again
            await IOUtils.ReadBytesFully(reader, readBuffer, 3, 2);

            // assert
            ComparisonUtils.CompareData(new byte[] { 6, 7 }, 0, 2,
                readBuffer, 3, 2);

            // test zero byte reads.
            readBuffer = new byte[] { 2, 3, 5, 8 };
            await IOUtils.ReadBytesFully(reader, readBuffer, 0, 0);
            Assert.Equal(new byte[] { 2, 3, 5, 8 }, readBuffer);
        }

        [Fact]
        public async Task TestReadBytesFullyForErrors()
        {
            // arrange
            var reader = new RandomizedReadSizeBufferReader(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[5];

            // act
            await IOUtils.ReadBytesFully(reader, readBuffer, 0, readBuffer.Length);

            // assert
            ComparisonUtils.CompareData(
                new byte[] { 0, 1, 2, 3, 4 }, 0, readBuffer.Length,
                readBuffer, 0, readBuffer.Length);

            // act and assert unexpected end of read
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
                IOUtils.ReadBytesFully(reader, readBuffer, 0, readBuffer.Length));
            Assert.Contains("end of read", actualEx.Message);
        }

        [Theory]
        [MemberData(nameof(CreateTestReadAllBytesData))]
        public async Task TestReadAllBytes(int bufferingLimit, int readBufferSize,
            byte[] expected)
        {
            // arrange
            var reader = new RandomizedReadSizeBufferReader(expected);
            
            // act
            var actual = await IOUtils.ReadAllBytes(reader, bufferingLimit,
                readBufferSize);
            
            // assert
            Assert.Equal(expected, actual);

            // assert that reader has been exhausted.
            Assert.Equal(0, await reader.ReadBytes(new byte[1], 0, 1));
        }

        public static List<object[]> CreateTestReadAllBytesData()
        {
            var testData = new List<object[]>();

            int bufferingLimit = 0;
            int readBufferSize = 0;
            byte[] expected = new byte[0];
            testData.Add(new object[] { bufferingLimit, readBufferSize, expected });

            bufferingLimit = 0;
            readBufferSize = 0;
            expected = new byte[] { 2 };
            testData.Add(new object[] { bufferingLimit, readBufferSize, expected });

            bufferingLimit = 6;
            readBufferSize = 6;
            expected = new byte[] { 0, 1, 2, 5, 6, 7 };
            testData.Add(new object[] { bufferingLimit, readBufferSize, expected });

            bufferingLimit = 6;
            readBufferSize = 2;
            expected = new byte[] { 0, 1, 4, 5, 6, 7 };
            testData.Add(new object[] { bufferingLimit, readBufferSize, expected });

            bufferingLimit = 10;
            readBufferSize = 3;
            expected = new byte[] { 0, 1, 2, 4, 5, 6, 7, 9 };
            testData.Add(new object[] { bufferingLimit, readBufferSize, expected });

            bufferingLimit = -1;
            readBufferSize = -1;
            expected = new byte[] { 3, 0, 1, 2, 4, 5, 6, 7, 9, 8, 10, 11, 12,
                113, 114 };
            testData.Add(new object[] { bufferingLimit, readBufferSize, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadAllBytesForErrorsData))]
        public async Task TestReadAllBytesForErrors(byte[] srcData,
            int bufferingLimit, int readBufferSize)
        {
            // arrange
            var reader = new RandomizedReadSizeBufferReader(srcData);

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
                IOUtils.ReadAllBytes(reader, bufferingLimit, readBufferSize));

            // assert
            Assert.Contains($"limit of {bufferingLimit}", actualEx.Message);
        }

        public static List<object[]> CreateTestReadAllBytesForErrorsData()
        {
            var testData = new List<object[]>();

            byte[] srcData = new byte[] { 0, 1, 2, 5, 6, 7 };
            int bufferingLimit = 5;
            int readBufferSize = 6;
            testData.Add(new object[] { srcData, bufferingLimit, readBufferSize });

            srcData = new byte[] { 0, 1, 2, 4, 5, 6, 7, 9 };
            bufferingLimit = 7;
            readBufferSize = 0;
            testData.Add(new object[] { srcData, bufferingLimit, readBufferSize });

            srcData = new byte[]{ 0, 1, 2, 3, 4, 5, 6, 7, 9 };
            bufferingLimit = 8;
            readBufferSize = 3;
            testData.Add(new object[] { srcData, bufferingLimit, readBufferSize });

            return testData;
        }

        [InlineData("", 0, true, true)]
        [InlineData("ab", 1, true, false)]
        [InlineData("ab", 1, false, false)]
        [InlineData("ab", 2, false, true)]
        [InlineData("abc", 2, false, false)]
        [InlineData("abcd", 3, true, true)]
        [InlineData("abcde", 0, true, false)]
        [InlineData("abcde", 0, false, false)]
        [InlineData("abcdef", -1, false, true)]
        [Theory]
        public async Task TestCopyBytes(string srcData, int readBufferSize,
            bool wrapReaderStream, bool wrapWriterStream)
        {
            // arrange
            var expected = ByteUtils.StringToBytes(srcData);
            var readerStream = new MemoryStream(expected);
            var readerStreamWrapper = new RandomizedReadSizeBufferReader(expected);
            var writerStream = new MemoryStream();
            var writerStreamWrapper = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    writerStream.WriteAsync(data, offset, length)
            };

            // act
            await IOUtils.CopyBytes(wrapReaderStream ? readerStreamWrapper :
                readerStream, wrapWriterStream ? writerStreamWrapper :
                writerStream, readBufferSize);

            // assert
            Assert.Equal(expected, writerStream.ToArray());
        }
    }
}
