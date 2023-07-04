using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class CustomReaderBackedBodyTest
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
            var instance = new CustomReaderBackedBody(
                new StreamCustomReaderWriter(stream));

            // act and assert
            await IOUtilsTest.TestReading(instance, null, 2, expected, null);
            Assert.Equal(-1, instance.ContentLength);
        }

        [Fact]
        public async Task TestCustomDispose()
        {
            var expected = Encoding.UTF8.GetBytes("c,2\n");
            var stream = new MemoryStream(expected);
            var instance = new CustomReaderBackedBody(
                new StreamCustomReaderWriter(stream));

            Assert.Equal(-1, instance.ContentLength);

            var actual = new byte[3];
            var actualLen = await instance.ReadBytes(actual, 0, 3);
            Assert.Equal(3, actualLen);
            ComparisonUtils.CompareData(expected, 0, actualLen,
                actual, 0, actualLen);

            // verify custom dispose is called on stream reader.
            await instance.CustomDispose();

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                instance.ReadBytes(actual, 1, 2));
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new CustomReaderBackedBody(null);
            });
        }
    }
}
