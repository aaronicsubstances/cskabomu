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
        public async Task TestReadBytesFully()
        {
            // arrange
            var reader = new HelperCustomReaderWritable(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
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
            var reader = new HelperCustomReaderWritable(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 });
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

            // assert that reader hasn't been disposed
            await reader.ReadBytes(readBuffer, 0, readBuffer.Length);
        }

        [Theory]
        [MemberData(nameof(CreateTestReadAllBytesData))]
        public async Task TestReadAllBytes(int bufferingLimit, int readBufferSize,
            byte[] expected)
        {
            // arrange
            var reader = new HelperCustomReaderWritable(expected);
            
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
            var reader = new HelperCustomReaderWritable(srcData);

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
            var reader = new HelperCustomReaderWritable(
                ByteUtils.StringToBytes(srcData))
            {
                TurnOffReadRandomization = true
            };

            // act and assert
            await TestReading(reader, null, readBufferSize, expected, null);
        }

        internal static async Task TestReading(ICustomReader reader, ICustomWriter writer,
            int readBufferSize, string expected,
            Func<ICustomWriter, string> actualFunc)
        {
            // arrange
            if (writer == null)
            {
                writer = new DemoCustomReaderWriter(null, ",");
                actualFunc = w => ByteUtils.BytesToString(
                    ((DemoCustomReaderWriter)w).BufferStream.ToArray());
            }

            // act.
            await IOUtils.CopyBytes(reader, writer, readBufferSize);

            // assert
            Assert.Equal(expected, actualFunc.Invoke(writer));

            // ensure reader or writer weren't disposed.
            await writer.WriteBytes(new byte[0], 0, 0);
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
            var reader = new HelperCustomReaderWritable(expected);
            ICustomWritable fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = null;
            reader = null;
            fallback = null;
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3 };
            reader = new HelperCustomReaderWritable(expected);
            fallback = new LambdaBasedCustomWritable();
            testData.Add(new object[] { reader, fallback, expected });

            expected = new byte[] { 0, 1, 2, 3, 4, 5 };
            reader = null;
            fallback = new HelperCustomReaderWritable(expected);
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

        class HelperCustomReaderWritable : ICustomReader, ICustomWritable
        {
            private readonly Random _randGen = new Random();
            private readonly MemoryStream _stream;

            public HelperCustomReaderWritable() :
                this(null)
            {
            }

            public HelperCustomReaderWritable(byte[] srcData)
            {
                _stream = new MemoryStream(srcData ?? new byte[0]);
                _stream.Position = 0; // rewind for reading
            }

            public bool TurnOffReadRandomization { get; set; }

            public Task<int> ReadBytes(byte[] data, int offset, int length)
            {
                var bytesToCopy = (int)Math.Min(_stream.Length - _stream.Position, length);
                if (bytesToCopy > 0)
                {
                    if (!TurnOffReadRandomization)
                    {
                        // copy just a random quantity out of the remaining bytes
                        bytesToCopy = _randGen.Next(bytesToCopy) + 1;
                    }
                }
                return _stream.ReadAsync(data, offset, bytesToCopy);
            }

            public Task CustomDispose()
            {
                _stream.Dispose();
                return Task.CompletedTask;
            }

            public Task WriteBytesTo(ICustomWriter writer)
            {
                var srcDataLen = (int)_stream.Length; // should trigger exception if disposed
                return writer.WriteBytes(_stream.ToArray(), 0, srcDataLen);
            }
        }

        class HelperCustomWriter : ICustomWriter
        {
            private readonly MemoryStream _stream = new MemoryStream();

            public MemoryStream BufferStream => _stream;

            public async Task WriteBytes(byte[] data, int offset, int length)
            {
                await _stream.WriteAsync(data, offset, length);
            }

            public Task CustomDispose()
            {
                _stream.Dispose();
                return Task.CompletedTask;
            }
        }
    }
}
