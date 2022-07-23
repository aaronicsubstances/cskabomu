using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class StreamBackedBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var backingStream = new MemoryStream();
            var instance = new StreamBackedBody(backingStream, 0)
            {
                ContentType = "text/csv"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestEmptyReadWithExcessData()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[4]);
            var instance = new StreamBackedBody(backingStream, 0)
            {
                ContentType = "text/csv"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyReadWithoutContentLength()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b', (byte)'2' });
            var instance = new StreamBackedBody(backingStream)
            {
                ContentType = "application/octet-stream"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "application/octet-stream",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, 3)
            {
                ContentType = "image/png"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "image/png",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestNonEmptyReadWithExcessData()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b', (byte)'2', 0 });
            var instance = new StreamBackedBody(backingStream, 3);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, null,
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestWithEmptyBodyWhichCannotCompleteReads()
        {
            // arrange.
            var backingStream = new MemoryStream();
            var instance = new StreamBackedBody(backingStream, 1)
            {
                ContentType = "text/csv"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 1, "text/csv",
                new int[0], "before end of read", null);
        }

        [Fact]
        public Task TestWithBodyWhichCannotCompleteReads()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b' });
            var instance = new StreamBackedBody(backingStream, 3);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, null,
                new int[] { 2 }, "before end of read", null);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new StreamBackedBody(null, -1);
            });
            var backingStream = new MemoryStream(new byte[] { (byte)'c', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, -1);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
