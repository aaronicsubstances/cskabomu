using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class IOUtilsTest
    {
        [Fact]
        public async Task TestReadBytesFully()
        {
            // arrange
            var reader = new DemoCustomReaderWritable(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
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

            // assert that reader hasn't been disposed
            await reader.ReadBytes(new byte[1], 0, 1);
        }

        [Fact]
        public async Task TestReadBytesFullyForErrors()
        {
            // arrange
            var reader = new DemoCustomReaderWritable(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
            var readBuffer = new byte[5];

            // act
            await IOUtils.ReadBytesFully(reader, readBuffer, 0, readBuffer.Length);

            // assert
            ComparisonUtils.CompareData(
                new byte[] { 0, 1, 2, 3, 4 }, 0, readBuffer.Length,
                readBuffer, 0, readBuffer.Length);

            // act and assert unexpected end of read
            await Assert.ThrowsAsync<EndOfReadException>(() =>
                IOUtils.ReadBytesFully(reader, readBuffer, 0, readBuffer.Length));

            // assert that reader hasn't been disposed
            await reader.ReadBytes(readBuffer, 0, readBuffer.Length);
        }

        [Theory]
        [MemberData(nameof(CreateTestReadAllBytesData))]
        public async Task TestReadAllBytes(int bufferingLimit, int readBufferSize,
            byte[] expected)
        {
            // arrange
            var reader = new DemoCustomReaderWritable(expected);
            
            // act
            var actual = await IOUtils.ReadAllBytes(reader, bufferingLimit,
                readBufferSize);
            
            // assert
            // check that reader has been disposed.
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadBytes(new byte[1], 0, 1));
            // finally verify content.
            Assert.Equal(expected, actual);
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
            var reader = new DemoCustomReaderWritable(srcData);

            // act
            var actualEx = await Assert.ThrowsAsync<DataBufferLimitExceededException>(() =>
                IOUtils.ReadAllBytes(reader, bufferingLimit, readBufferSize));

            // assert
            Assert.Equal(bufferingLimit, actualEx.BufferSizeLimit);
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

        [InlineData("", 0, "")]
        [InlineData("ab", 1, "a,b,")]
        [InlineData("ab", 2, "ab,")]
        [InlineData("abc", 2, "ab,c,")]
        [InlineData("abcd", 3, "abc,d,")]
        [InlineData("abcde", 0, "abcde,")]
        [InlineData("abcdef", -1, "abcdef,")]
        [Theory]
        public async Task TestCopyBytes(string srcData, int readBufferSize, string expected)
        {
            // arrange
            var reader = new DemoCustomReaderWritable(Encoding.UTF8.GetBytes(srcData))
            {
                TurnOffRandomization = true
            };
            var actual = new StringBuilder();
            var disposed = false;
            var writer = new LambdaBasedCustomWriter
            {
                WriteFunc = (data, offset, length) =>
                {
                    if (disposed)
                    {
                        throw new ObjectDisposedException("writer");
                    }
                    actual.Append(Encoding.UTF8.GetString(data, offset, length));
                    actual.Append(",");
                    return Task.CompletedTask;
                },
                DisposeFunc = () =>
                {
                    disposed = true;
                    return Task.CompletedTask;
                }
            };

            // act.
            await IOUtils.CopyBytes(reader, writer, readBufferSize);

            // assert
            Assert.Equal(expected, actual.ToString());

            // ensure reader or writer weren't disposed.
            await writer.WriteFunc(new byte[0], 0, 0);
            await reader.ReadBytes(new byte[0], 0, 0);
        }

        [Theory]
        [MemberData(nameof(CreateTestCoalesceAsReaderData))]
        public async Task TestCoalesceAsReader(ICustomReader reader,
            ICustomWritable fallback, byte[] expected)
        {
            reader = IOUtils.CoalesceAsReader(reader, fallback);
            byte[] actual = null;
            if (reader != null)
            {
                var desired = IOUtils.ReadAllBytes(reader, 0, 2);
                // just in case error causes desired to hang forever,
                // impose timeout
                var first = await Task.WhenAny(Task.Delay(3000),
                    desired);
                if (first == desired)
                {
                    actual = await desired;
                }
            }
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestCoalesceAsReaderData()
        {
            var testData = new List<object[]>();

            var expected = new byte[] { };
            var reader = new DemoCustomReaderWritable(expected);
            ICustomWritable fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = null;
            reader = null;
            fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3 };
            reader = new DemoCustomReaderWritable(expected);
            fallback = new LambdaBasedCustomWritable();
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3, 4, 5 };
            reader = null;
            fallback = new DemoCustomReaderWritable(expected);
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2 };
            reader = null;
            fallback = new LambdaBasedCustomWritable
            {
                WritableFunc = writer =>
                {
                    return writer.WriteBytes(expected, 0, expected.Length);
                }
            };
            testData.Add(new object[] { reader, fallback, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestCoaleasceAsWritableData))]
        public async Task TestCoaleasceAsWritable(ICustomWritable writable,
            ICustomReader fallback, string expected)
        {
            writable = IOUtils.CoaleasceAsWritable(writable, fallback);
            StringBuilder actual = null;
            if (writable != null)
            {
                actual = new StringBuilder();
                ICustomWriter writer = new LambdaBasedCustomWriter
                {
                    WriteFunc = (data, offset, length) =>
                    {
                        actual.Append(Encoding.UTF8.GetString(data, offset, length));
                        return Task.CompletedTask;
                    }
                };
                await writable.WriteBytesTo(writer);
            }
            Assert.Equal(expected, actual?.ToString());
        }

        public static List<object[]> CreateTestCoaleasceAsWritableData()
        {
            var testData = new List<object[]>();

            var expected = "";
            var writable = new DemoCustomReaderWritable(
                Encoding.UTF8.GetBytes(expected));
            ICustomReader fallback = null;
            testData.Add(new object[] { writable, fallback, expected });

            expected = null;
            writable = null;
            fallback = null;
            testData.Add(new object[] { writable, fallback, expected });

            expected = "abcdef";
            writable = new DemoCustomReaderWritable(
                Encoding.UTF8.GetBytes(expected));
            fallback = new LambdaBasedCustomReader();
            testData.Add(new object[] { writable, fallback, expected });

            expected = "ghijklmnop";
            writable = null;
            fallback = new DemoCustomReaderWritable(
                Encoding.UTF8.GetBytes(expected));
            testData.Add(new object[] { writable, fallback, expected });

            expected = "qrstuvwxyz";
            writable = null;
            fallback = new DemoSimpleCustomReader(
                Encoding.UTF8.GetBytes(expected));
            testData.Add(new object[] { writable, fallback, expected });

            return testData;
        }
    }
}
