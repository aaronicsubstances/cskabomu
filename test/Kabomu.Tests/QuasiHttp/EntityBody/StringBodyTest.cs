using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class StringBodyTest
    {
        [InlineData("", 0, "")]
        [InlineData("ab", 2, "ab,")]
        [InlineData("abc", 3, "ab,c,")]
        [InlineData("abcd", 4, "ab,cd,")]
        [InlineData("abcde", 5, "ab,cd,e,")]
        [InlineData("abcdef", 6, "ab,cd,ef,")]
        [InlineData("Foo \u00c0\u00ff", 8, "Fo,o ,\u00c0,\u00ff,")]
        [Theory]
        public async Task TestReading(string srcData, int expectedContentLength,
            string expected)
        {
            // arrange
            var instance = new StringBody(srcData);

            // act and assert
            Assert.Equal(expectedContentLength, instance.ContentLength);
            await IOUtilsTest.TestReading(instance.Reader(), null, 2, expected, null);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var expected = ByteUtils.StringToBytes("c,2\n");
            var instance = new StringBody("c,2\n");

            // verify custom dispose is a no-op
            await instance.Release();

            var reader = instance.Reader();

            var actual = new byte[3];
            var actualLen = await reader.ReadBytes(actual, 0, 3);
            Assert.Equal(3, actualLen);
            ComparisonUtils.CompareData(expected, 0, actualLen,
                actual, 0, actualLen);

            // verify custom dispose is a no-op
            await instance.Release();

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
        [InlineData("Foo \u00c0\u00ff")]
        public async Task TestWriting(string expected)
        {
            var instance = new StringBody(expected);
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
                new StringBody(null);
            });
        }
    }
}
