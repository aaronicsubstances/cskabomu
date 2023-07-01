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
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(srcData));
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
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(srcData));
            ICustomReader instance = new StreamCustomReaderWriter(stream);
            instance = new ContentLengthEnforcingCustomReader(instance, contentLength);

            // act and assert
            var expectedEx = await Assert.ThrowsAsync<ContentLengthNotSatisfiedException>(
                () => IOUtilsTest.TestReading(instance, null, 0,
                    null, null));
            Assert.Equal(contentLength, expectedEx.ContentLength);
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

            // memory stream may not bother reacting to
            // Dispose() call. However thanks to cancellation
            // handle used inside implementation, that handle
            // is able to help detect calls to Dispose().
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => instance.ReadBytes(new byte[0], 0, 0));
        }
    }
}
