using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class ByteBufferBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[0], 0, 0, "text/plain");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/plain",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[] { (byte)'A', (byte)'b', (byte)'2' }, 0, 3, null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "application/octet-stream",
                new int[] { 2, 1 }, null, instance.Buffer);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ByteBufferBody(new byte[] { 0, 0 }, 1, 2, null);
            });
            var instance = new ByteBufferBody(new byte[] { 0, 0, 0 }, 1, 2, null);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
