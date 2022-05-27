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
            CommonBodyTestRunner.RunCommonBodyTest(instance, 0, "text/plain",
                new int[0], null, "");
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[] { (byte)'A', (byte)'b', (byte)'2' }, 0, 3, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 3, "application/octet-stream",
                new int[] { 2, 1 }, null, "Ab2");
        }
    }
}
