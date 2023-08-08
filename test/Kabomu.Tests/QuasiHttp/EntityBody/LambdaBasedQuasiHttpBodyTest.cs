using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class LambdaBasedQuasiHttpBodyTest
    {
        [Theory]
        [InlineData("")]
        [InlineData("d")]
        [InlineData("ds")]
        [InlineData("data")]
        [InlineData("datadriven")]
        public async Task TestWriting(string expected)
        {
            // arrange
            var instance = new CustomWritableBackedBody(
                new DemoSimpleCustomWritable(ByteUtils.StringToBytes(expected)));
            var writer = new DemoCustomReaderWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.BufferStream.ToArray()));
            Assert.Equal(-1, instance.ContentLength);
        }

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
            var stream = new MemoryStream(ByteUtils.StringToBytes(srcData));
            var instance = new CustomReaderBackedBody(
                new StreamCustomReaderWriter(stream));

            // act and assert
            await IOUtilsTest.TestReading(instance.Reader(), null, 2, expected, null);
            Assert.Equal(-1, instance.ContentLength);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var instance = new CustomWritableBackedBody(
                new DemoSimpleCustomWritable(ByteUtils.StringToBytes("c,2\n")));
            var writer = new DemoCustomReaderWriter();

            // verify custom dispose is called on writable.
            await instance.Release();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                instance.WriteBytesTo(writer));
        }
    }
}
