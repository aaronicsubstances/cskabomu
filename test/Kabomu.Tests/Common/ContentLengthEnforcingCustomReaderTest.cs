using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class ContentLengthEnforcingCustomReaderTest
    {
        [InlineData(0, "", "")]
        [InlineData(0, "a", "")]
        [InlineData(1, "ab", "a")]
        [InlineData(-2, "ab", "ab")]
        [InlineData(2, "abc", "ab")]
        [InlineData(3, "abc", "abc")]
        [InlineData(4, "abcd", "abcd")]
        [InlineData(5, "abcde", "abcde")]
        [InlineData(-1, "abcdef", "abcdef")]
        [Theory]
        public async Task TestReading(long contentLength, string srcData,
            string expected)
        {
            // arrange
            var stream = new MemoryStream(ByteUtils.StringToBytes(srcData));
            var instance = new ContentLengthEnforcingCustomReader(stream,
                contentLength);

            // act
            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance));

            // assert
            Assert.Equal(expected, actual);

            // assert non-repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance));
            Assert.Equal("", actual);
        }

        [InlineData(2, "")]
        [InlineData(4, "abc")]
        [InlineData(5, "abcd")]
        [InlineData(15, "abcdef")]
        [Theory]
        public async Task TestReadingForErrors(long contentLength, string srcData)
        {
            // arrange
            var stream = new MemoryStream(ByteUtils.StringToBytes(srcData));
            var instance = new ContentLengthEnforcingCustomReader(stream,
                contentLength);

            // act and assert
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(
                () => IOUtils.ReadAllBytes(
                instance));
            Assert.Contains($"length of {contentLength}", actualEx.Message);
        }

        [Fact]
        public async Task TestZeroByteRead1()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2});
            var instance = new ContentLengthEnforcingCustomReader(stream, -1);

            var actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            var expected = new byte[3];
            actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2});

            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);
        }

        [Fact]
        public async Task TestZeroByteRead2()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            var instance = new ContentLengthEnforcingCustomReader(stream, 3);

            var actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            var expected = new byte[3];
            actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2 });

            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);
        }

        [Fact]
        public async Task TestZeroByteRead3()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                {
                    if (length == 0)
                    {
                        throw new ArgumentException("this instance only accepts " +
                            "positive lengths");
                    }
                    return stream.ReadAsync(data, offset, length);
                }
            };
            var instance = new ContentLengthEnforcingCustomReader(reader, 4);
            int actual;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));

            var expected = new byte[3];
            actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2 });

            await Assert.ThrowsAsync<CustomIOException>(
                () => instance.ReadBytes(new byte[2], 0, 2));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));
        }

        [Fact]
        public async Task TestZeroByteRead4()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                {
                    if (length == 0)
                    {
                        throw new ArgumentException("this instance only accepts " +
                            "positive lengths");
                    }
                    return stream.ReadAsync(data, offset, length);
                }
            };
            var instance = new ContentLengthEnforcingCustomReader(reader, -1);
            int actual;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));

            var expected = new byte[3];
            actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2 });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));

            actual = await instance.ReadBytes(new byte[2], 0, 2);
            Assert.Equal(0, actual);
        }

        [Fact]
        public async Task TestZeroByteRead5()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2, 3 });
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                {
                    if (length == 0)
                    {
                        throw new ArgumentException("this instance only accepts " +
                            "positive lengths");
                    }
                    return stream.ReadAsync(data, offset, length);
                }
            };
            var instance = new ContentLengthEnforcingCustomReader(reader, 3);
            int actual;

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));

            var expected = new byte[3];
            actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2 });

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));

            actual = await instance.ReadBytes(new byte[2], 0, 2);
            Assert.Equal(0, actual);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                instance.ReadBytes(new byte[0], 0, 0));
        }
    }
}
