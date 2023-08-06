using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class ByteBufferBodyTest
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
            var instance = new ByteBufferBody(ByteUtils.StringToBytes(srcData));

            // act and assert
            await IOUtilsTest.TestReading(instance.Reader(), null, 2, expected, null);
            Assert.Equal(srcData.Length, instance.ContentLength);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var expected = ByteUtils.StringToBytes("c,2\n");
            var instance = new ByteBufferBody(expected);

            Assert.Equal(expected.Length, instance.ContentLength);

            // verify custom dispose is a no-op
            await instance.CustomDispose();

            var reader = instance.Reader();

            var actual = new byte[3];
            var actualLen = await reader.ReadBytes(actual, 0, actual.Length);
            Assert.Equal(3, actualLen);
            ComparisonUtils.CompareData(expected, 0, actualLen,
                actual, 0, actualLen);

            // verify custom dispose is a no-op
            await instance.CustomDispose();

            actualLen = await reader.ReadBytes(actual, 1, 2);
            Assert.Equal(1, actualLen);
            ComparisonUtils.CompareData(expected, 3, actualLen,
                actual, 1, actualLen);

            await reader.CustomDispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                reader.ReadBytes(actual, 0, actual.Length));
        }

        [Theory]
        [InlineData("")]
        [InlineData("d")]
        [InlineData("ds")]
        [InlineData("data")]
        [InlineData("datadriven")]
        public async Task TestWriting(string expected)
        {
            var instance = new ByteBufferBody(ByteUtils.StringToBytes(expected));
            var writer = new DemoCustomReaderWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.BufferStream.ToArray()));
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ByteBufferBody(null, 1, 2);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new ByteBufferBody(new byte[] { 0, 0 }, 1, 2);
            });
        }
    }
}
