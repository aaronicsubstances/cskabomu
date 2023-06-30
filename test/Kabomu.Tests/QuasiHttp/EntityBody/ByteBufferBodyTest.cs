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
    public class ByteBufferBodyTest
    {
        /*[Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[0])
            {
                ContentType = "text/plain"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/plain",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestEmptyReadWithExcessData()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[4])
            {
                ContentLength = 0,
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
            var backingData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new ByteBufferBody(backingData)
            {
                ContentLength = -1,
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
            var instance = new ByteBufferBody(new byte[] { (byte)'A', (byte)'b', (byte)'2' }, 0, 3)
            {
                ContentType = "application/octet-stream"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "application/octet-stream",
                new int[] { 2, 1 }, null, instance.Buffer);
        }

        [Fact]
        public Task TestNonEmptyReadWithExcessData()
        {
            // arrange.
            var backingData = new byte[] { (byte)'A', (byte)'b', (byte)'2', 0 };
            var instance = new ByteBufferBody(backingData, 0, backingData.Length)
            {
                ContentLength = 3
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, null,
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestWithEmptyBodyWhichCannotCompleteReads()
        {
            // arrange.
            var instance = new ByteBufferBody(new byte[0], 0, 0)
            {
                ContentLength = 1,
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
            var backingData = new byte[] { (byte)'A', (byte)'b' };
            var instance = new ByteBufferBody(backingData)
            {
                ContentLength = 3
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, null,
                new int[] { 2 }, "before end of read", null);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ByteBufferBody(null, 1, 2);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new ByteBufferBody(new byte[] { 0, 0 }, 1, 2);
            });
            var instance = new ByteBufferBody(new byte[] { 0, 0, 0 }, 1, 2);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }*/
    }
}
