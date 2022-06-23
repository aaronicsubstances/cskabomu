using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class ChunkEncodingBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var wrappedBody = new StringBody("", "text/csv");
            var instance = new ChunkEncodingBody(wrappedBody, 100);
            var expectedSuccessData = new byte[] { 0, 0, 2, 1, 0 };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(6, instance, -1, "text/csv",
                new int[] { 5 }, null, expectedSuccessData);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var wrappedBody = new ByteBufferBody(new byte[] { 4, 5, 6, 7 }, 0, 4, "image/gif");
            var instance = new ChunkEncodingBody(wrappedBody, 100);
            var expectedSuccessData = new byte[] { 0, 0, 5, 1, 0, 4, 5, 6, 0, 0, 3, 1, 0, 7, 0, 0, 2, 1, 0 };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(8, instance, -1, "image/gif",
                new int[] { 8, 6, 5 }, null, expectedSuccessData);
        }

        [Fact]
        public async Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkEncodingBody(null, 100);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkEncodingBody(new StringBody("abc", null), int.MaxValue);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                var instance = new ChunkEncodingBody(new StringBody("3", "text/html"), 100);
                return instance.ReadBytes(new byte[4], 0, 4);
            });
            var instance = new ChunkEncodingBody(new StringBody("", null), 20);
            await CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
