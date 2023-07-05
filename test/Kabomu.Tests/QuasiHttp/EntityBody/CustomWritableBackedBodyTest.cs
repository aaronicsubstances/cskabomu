using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class CustomWritableBackedBodyTest
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

        [Fact]
        public async Task TestCustomDispose()
        {
            var instance = new CustomWritableBackedBody(
                new DemoSimpleCustomWritable(ByteUtils.StringToBytes("c,2\n")));
            var writer = new DemoCustomReaderWriter();

            // verify custom dispose is called on writable.
            await instance.CustomDispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                instance.WriteBytesTo(writer));
        }
    }
}
