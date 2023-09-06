using Kabomu.Common;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public async Task TestReadAllBytes(int bufferingLimit, byte[] expected)
        {
            // arrange
            var reader = new RandomizedReadSizeBufferReader(expected);
            
            // act
            var actual = await IOUtils.ReadAllBytes(reader, bufferingLimit);
            
            // assert
            Assert.Equal(expected, actual);

            // assert that reader has been exhausted.
            Assert.Equal(0, await reader.ReadBytes(new byte[1], 0, 1));
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
            var reader = new RandomizedReadSizeBufferReader(srcData);

            // act
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(() =>
                IOUtils.ReadAllBytes(reader, bufferingLimit));

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

            srcData = new byte[]{ 0, 1, 2, 3, 4, 5, 6, 7, 9 };
            bufferingLimit = 8;
            testData.Add(new object[] { srcData, bufferingLimit });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestCopyBytesData))]
        public async Task TestCopyBytesWithStreams(string srcData)
        {
            // arrange
            var expected = MiscUtils.StringToBytes(srcData);
            var readerStream = new MemoryStream(expected);
            var writerStream = new MemoryStream();

            // act
            await IOUtils.CopyBytes(readerStream, writerStream);

            // assert
            Assert.Equal(expected, writerStream.ToArray());
        }

        [MemberData(nameof(CreateTestCopyBytesData))]
        [Theory]
        public async Task TestCopyBytesWithRemainingBytes(string srcData)
        {
            // arrange
            var expected = MiscUtils.StringToBytes(srcData);

            // double the expectation and read half way,
            // to test that remaining bytes are correctly copied
            var reader = new RandomizedReadSizeBufferReader(
                expected.Concat(expected).ToArray());
            var temp = new byte[expected.Length];
            await IOUtils.ReadBytesFully(reader, temp, 0, temp.Length);
            Assert.Equal(expected, temp);
            
            // now continue to test copyBytes() on
            // remaining data
            var writerStream = new MemoryStream();
            var writerStreamWrapper = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    writerStream.WriteAsync(data, offset, length)
            };

            // act
            await IOUtils.CopyBytes(reader, writerStreamWrapper);

            // assert
            Assert.Equal(expected, writerStream.ToArray());

            // assert that reader has been exhausted.
            var actual2 = await IOUtils.ReadBytes(reader, new byte[1], 0, 1);
            Assert.Equal(0, actual2);
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
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    throw new Exception("broken!")
            };
            await IOUtils.CopyBytes(reader, writer);
        }

        [Fact]
        public async Task TestCopyBytesForErrors1()
        {
            var reader = new MemoryStream(new byte[17]);
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    throw new Exception("broken!")
            };

            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                IOUtils.CopyBytes(reader, writer));
            Assert.Equal("broken!", actualEx.Message);
        }

        [Fact]
        public async Task TestCopyBytesForErrors2()
        {
            var firstReader = new MemoryStream(new byte[2000]); 
            var readerWrapper = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = async (data, offset, length) =>
                {
                    var result = firstReader.Read(data, offset, length);
                    if (result > 0)
                    {
                        return result;
                    }
                    throw new Exception("killed in action");
                }
            };
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    Task.CompletedTask
            };

            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                IOUtils.CopyBytes(readerWrapper, writer));
            Assert.Equal("killed in action", actualEx.Message);
        }

        [Fact]
        public async Task TestCopyBytesForErrors3()
        {
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = async (data, offset, length) =>
                {
                    throw new Exception("killed in action");
                }
            };
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    throw new Exception("broken!")
            };

            var actualEx = await Assert.ThrowsAsync<Exception>(() =>
                IOUtils.CopyBytes(reader, writer));
            Assert.Equal("killed in action", actualEx.Message);
        }
    }
}
