using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class ByteBufferBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[0], 0, 0, "text/plain");

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(0, instance, "text/plain",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[] { (byte)'A', (byte)'b', (byte)'2' }, 0, 3, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(2, instance, "application/octet-stream",
                new int[] { 2, 1 }, null, instance.Buffer);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ByteBufferBody(new byte[] { 0, 0 }, 1, 2, null);
            });
            var instance = new ByteBufferBody(new byte[] { 0, 0, 0 }, 1, 2, null);
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
