using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class StringBodyTest
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
            var instance = new StringBody(srcData);

            // act and assert
            await IOUtilsTest.TestReading(instance, null, 2, expected, null);
            Assert.Equal(srcData.Length, instance.ContentLength);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var expected = Encoding.UTF8.GetBytes("c,2\n");
            var instance = new StringBody("c,2\n");

            Assert.Equal(expected.Length, instance.ContentLength);

            // verify custom dispose is a no-op
            await instance.CustomDispose();

            var actual = new byte[3];
            var actualLen = await instance.ReadBytes(actual, 0, 3);
            Assert.Equal(3, actualLen);
            ComparisonUtils.CompareData(expected, 0, actualLen,
                actual, 0, actualLen);

            // verify custom dispose is a no-op
            await instance.CustomDispose();

            actualLen = await instance.ReadBytes(actual, 1, 2);
            Assert.Equal(1, actualLen);
            ComparisonUtils.CompareData(expected, 3, actualLen,
                actual, 1, actualLen);
        }

        [Theory]
        [InlineData("")]
        [InlineData("d")]
        [InlineData("ds")]
        [InlineData("data")]
        [InlineData("datadriven")]
        public async Task TestWriting(string expected)
        {
            var instance = new StringBody(expected);
            var writer = new DemoSimpleCustomWriter();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, writer.Buffer.ToString());
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
