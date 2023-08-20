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
    public class ByteBufferBodyTest
    {
        public static List<object[]> CreateTestData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[0], 0 },
                new object[]{ new byte[] { 50 }, 1 },
                new object[]{ new byte[] { 1, 2 }, 2 },
                new object[]{ new byte[] { 130, 148, 199 }, 3 },
                new object[]{ new byte[] { 97, 98, 99, 100 }, 4 },
            };
        }

        [MemberData(nameof(CreateTestData))]
        [Theory]
        public async Task TestReading(byte[] srcData, int expectedContentLength)
        {
            var instance = new ByteBufferBody(srcData);
            Assert.Equal(expectedContentLength, instance.ContentLength);

            var actual = await IOUtils.ReadAllBytes(instance.Reader);
            Assert.Equal(srcData, actual);

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            actual = await IOUtils.ReadAllBytes(instance.Reader);
            Assert.Equal(srcData, actual);
        }

        [Theory]
        [MemberData(nameof(CreateTestData))]
        public async Task TestWriting(byte[] srcData, int expectedContentLength)
        {
            var instance = new ByteBufferBody(srcData, 0, srcData.Length);
            Assert.Equal(expectedContentLength, instance.ContentLength);

            var writer = new MemoryStream();

            await instance.WriteBytesTo(writer);
            Assert.Equal(srcData, writer.ToArray());

            // verify that release is a no-op
            await instance.Release();

            // assert repeatability.
            writer.SetLength(0); // reset
            instance.ContentLength = -1; // should have no effect on expectations
            await instance.WriteBytesTo(writer);
            Assert.Equal(srcData, writer.ToArray());
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
