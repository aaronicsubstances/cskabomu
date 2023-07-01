using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class StreamCustomReaderWriterTest
    {
        [InlineData("", "")]
        [InlineData("ab", "ab,")]
        [InlineData("abc", "ab,c,")]
        [InlineData("abcd", "ab,cd,")]
        [InlineData("abcde", "ab,cd,e,")]
        [InlineData("abcdef", "ab,cd,ef,")]
        [Theory]
        public async Task TestReading(string srcData, string expected)
        {
            // arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(srcData));
            var instance = new StreamCustomReaderWriter(stream);

            // act and assert
            await IOUtilsTest.TestReading(instance, null, 2, expected, null);
        }

        [Theory]
        [InlineData("")]
        [InlineData("data")]
        public async Task TestWriting(string expected)
        {
            var reader = new DemoSimpleCustomReader(
                Encoding.UTF8.GetBytes(expected));
            var stream = new MemoryStream();
            var instance = new StreamCustomReaderWriter(stream);

            // act and assert
            await IOUtilsTest.TestReading(reader, instance, 2, expected,
                _ => Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var stream = new MemoryStream();
            var instance = new StreamCustomReaderWriter(stream);

            // ensure reader or writer weren't disposed.
            await instance.WriteBytes(new byte[0], 0, 0);
            await instance.ReadBytes(new byte[0], 0, 0);

            await instance.CustomDispose();

            // memory stream may not bother reacting to
            // Dispose() call. However thanks to cancellation
            // handle used inside implementation, that handle
            // is able to help detect calls to Dispose().
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => instance.WriteBytes(new byte[0], 0, 0));
            await Assert.ThrowsAsync<TaskCanceledException>(
                () => instance.ReadBytes(new byte[0], 0, 0));
        }
    }
}
