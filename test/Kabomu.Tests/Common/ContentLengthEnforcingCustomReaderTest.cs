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
        public async Task TestCustomDispose()
        {
            var stream = new MemoryStream();
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, 0);

            // ensure reader or writer weren't disposed.
            await instance.ReadBytes(new byte[0], 0, 0);

            await instance.CustomDispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => instance.ReadBytes(new byte[0], 0, 0));
        }
    }
}
