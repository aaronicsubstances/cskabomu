using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class ChunkEncodingBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var wrappedBody = new StringBody("", "text/csv");
            var instance = new ChunkEncodingBody(wrappedBody);
            var expectedSuccessData = new byte[] { 0, 2, 1, 0 };

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(5, instance, -1, "text/csv",
                new int[] { 4 }, null, expectedSuccessData);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var wrappedBody = new ByteBufferBody(new byte[] { 4, 5, 6, 7 }, 0, 4, "image/gif");
            var instance = new ChunkEncodingBody(wrappedBody);
            var expectedSuccessData = new byte[] { 0, 5, 1, 0, 4, 5, 6, 0, 3, 1, 0, 7, 0, 2, 1, 0 };

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(7, instance, -1, "image/gif",
                new int[] { 7, 5, 4 }, null, expectedSuccessData);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkEncodingBody(null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                var instance = new ChunkEncodingBody(new StringBody("3", "text/html"));
                instance.ReadBytes(new TestEventLoopApi(), new byte[4], 0, 4, (e, len) => { });
            });
            var instance = new ChunkEncodingBody(new StringBody("", null));
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
