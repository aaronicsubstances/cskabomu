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
        [InlineData(1, "ab", "a,")]
        [InlineData(-2, "ab", "ab,")]
        [InlineData(2, "abc", "ab,")]
        [InlineData(3, "abc", "ab,c,")]
        [InlineData(4, "abcd", "ab,cd,")]
        [InlineData(5, "abcde", "ab,cd,e,")]
        [InlineData(-1, "abcdef", "ab,cd,ef,")]
        [Theory]
        public async Task TestReading(long contentLength,
            string srcData, string expected)
        {
            // arrange
            var stream = new MemoryStream(ByteUtils.StringToBytes(srcData));
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, contentLength);

            // act and assert
            await IOUtilsTest.TestReading(instance, null, 2, expected, null);
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
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, contentLength);

            // act and assert
            var actualEx = await Assert.ThrowsAsync<CustomIOException>(
                () => IOUtilsTest.TestReading(instance, null, 0,
                    null, null));
            Assert.Contains($"length of {contentLength}", actualEx.Message);
        }

        [Fact]
        public async Task TestCustomDispose1()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2});
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, -1, true);

            var expected = new byte[3];
            var actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2});

            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            await instance.CustomDispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[0], 0, 0));

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCustomDispose2()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, -1);

            var expected = new byte[3];
            var actual = await instance.ReadBytes(expected, 0, 3);
            Assert.Equal(3, actual);
            Assert.Equal(expected, new byte[] { 0, 1, 2 });

            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            await instance.CustomDispose();

            // verify that zero byte can be initiated in spite of
            // disposal of backing reader.
            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCustomDispose3()
        {
            var stream = new MemoryStream(new byte[] { 0, 1, 2 });
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, 1, true);

            var actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            await instance.CustomDispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[0], 0, 0));

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCustomDispose4()
        {
            var stream = new MemoryStream(new byte[] { 4, 91, 2 });
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, 2, false);

            var actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            var expected = new byte[1];
            actual = await instance.ReadBytes(expected, 0, 1);
            Assert.Equal(1, actual);
            Assert.Equal(expected, new byte[] { 4 });

            await instance.CustomDispose();

            // verify that zero byte can be initiated in spite of
            // disposal of backing reader.
            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestCustomDispose5()
        {
            var stream = new MemoryStream(new byte[] { 4, 91, 2 });
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, 0);

            var actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);

            actual = await instance.ReadBytes(new byte[1], 0, 1);
            Assert.Equal(0, actual);

            await instance.CustomDispose();

            // verify that zero byte can be initiated in spite of
            // disposal of backing reader.
            actual = await instance.ReadBytes(new byte[0], 0, 0);
            Assert.Equal(0, actual);
            actual = await instance.ReadBytes(new byte[1], 0, 1);
            Assert.Equal(0, actual);
        }
    }
}
