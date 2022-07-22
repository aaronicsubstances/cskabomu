using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class ContentTypeOverrideBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var instance = new ContentTypeOverrideBody(new ByteBufferBody(new byte[0], 0, 0, "text/plain"),
                "image/jpeg");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "image/jpeg",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var expectedData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new ContentTypeOverrideBody(new ByteBufferBody(expectedData, 0, expectedData.Length, null),
                "application/octet-stream");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "application/octet-stream",
                new int[] { 2, 1 }, null, expectedData);
        }

        [Fact]
        public Task TestNonEmptyReadWithoutContentType()
        {
            // arrange.
            var expectedData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new ContentTypeOverrideBody(new ByteBufferBody(expectedData, 0, expectedData.Length, "form"),
                null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, null,
                new int[] { 2, 1 }, null, expectedData);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ContentTypeOverrideBody(null, "seal");
            });
            var instance = new ContentTypeOverrideBody(new ByteBufferBody(new byte[] { 0, 0, 0 }, 1, 2, null),
                null);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
