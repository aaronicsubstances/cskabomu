using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class StreamBackedBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var backingStream = new MemoryStream();
            var instance = new StreamBackedBody(backingStream, "text/csv");

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(0, instance, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(2, instance, "application/octet-stream",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StreamBackedBody(null, null);
            });
            var backingStream = new MemoryStream(new byte[] { (byte)'c', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, null);
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
