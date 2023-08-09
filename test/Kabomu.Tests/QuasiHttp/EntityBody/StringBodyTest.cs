using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class StringBodyTest
    {
        [InlineData("", 0)]
        [InlineData("ab", 2)]
        [InlineData("abc", 3)]
        [InlineData("abcd", 4)]
        [InlineData("abcde", 5)]
        [InlineData("abcdef", 6)]
        [InlineData("Foo \u00c0\u00ff", 8)]
        [Theory]
        public async Task TestReading(string srcData, int expectedContentLength)
        {
            // arrange
            var instance = new StringBody(srcData);
            Assert.Equal(expectedContentLength, instance.ContentLength);

            // act
            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));

            // assert
            Assert.Equal(srcData, actual);
            Assert.Equal(expectedContentLength, instance.ContentLength);
            
            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(srcData, actual);
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
            var instance = new StringBody(expected)
            {
                ContentLength = -1
            };
            var writer = new MemoryStream();

            // act
            await instance.WriteBytesTo(writer);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected + expected, ByteUtils.BytesToString(
                writer.ToArray()));
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
