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
        public static List<object[]> CreateTestData()
        {
            return new List<object[]>
            {
                new object[]{ "", 0 },
                new object[]{ "ab", 2 },
                new object[]{ "abc", 3 },
                new object[]{ "\u0001\u0019\u0020\u007e", 4 },
                new object[]{ "abcdef", 6 },
                new object[]{ "Foo \u00c0\u00ff", 8 }
            };
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestReading(string srcData, int expectedContentLength)
        {
            var instance = new StringBody(srcData);
            Assert.Equal(expectedContentLength, instance.ContentLength);

            var actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(srcData, actual);
            
            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            actual = ByteUtils.BytesToString(await IOUtils.ReadAllBytes(
                instance.Reader));
            Assert.Equal(srcData, actual);
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestWriting(string expected, int expectedContentLength)
        {
            var instance = new StringBody(expected);
            Assert.Equal(expectedContentLength, instance.ContentLength);

            var writer = new MemoryStream();

            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.ToArray()));

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            writer.SetLength(0); // reset
            instance.ContentLength = -1; // should have no effect on expectations
            await instance.WriteBytesTo(writer);
            Assert.Equal(expected, ByteUtils.BytesToString(
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
