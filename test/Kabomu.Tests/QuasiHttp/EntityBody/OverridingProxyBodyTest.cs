using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class OverridingProxyBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var instance = new OverridingProxyBody(new ByteBufferBody(new byte[0], 0, 0))
            {
                ContentType = "text/plain",
                ContentLength = 9, // should not take effect
                IsContentLengthProxied = true
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, "text/plain",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var expectedData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new OverridingProxyBody(new StringBody(
                ByteUtils.BytesToString(expectedData)));

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, null,
                new int[] { 2, 1 }, null, expectedData);
        }

        [Fact]
        public Task TestNonEmptyReadWithoutContentLength()
        {
            // arrange.
            var expectedData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new OverridingProxyBody(new ByteBufferBody(expectedData)
            {
                ContentType = "form"
            })
            {
                IsContentTypeProxied = true
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "form",
                new int[] { 2, 1 }, null, expectedData);
        }

        [Fact]
        public Task TestEmptyReadWithExcessData()
        {
            // arrange.
            var excessData = new byte[4];
            var instance = new OverridingProxyBody(new ByteBufferBody(excessData))
            {
                ContentType = "text/csv",
                ContentLength = 0
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyReadWithExcessData()
        {
            // arrange.
            var excessData = new byte[] { (byte)'A', (byte)'b', (byte)'2', 0 };
            var instance = new OverridingProxyBody(new ByteBufferBody(excessData))
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
            var instance = new OverridingProxyBody(new ByteBufferBody(new byte[0]))
            {
                ContentLength = 1
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 1, "text/plain",
                new int[0], "before end of read", null);
        }

        [Fact]
        public Task TestWithBodyWhichCannotCompleteReads()
        {
            // arrange.
            var insufficientData = new byte[] { (byte)'A', (byte)'b' };
            var instance = new OverridingProxyBody(new ByteBufferBody(insufficientData))
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
                new OverridingProxyBody(null);
            });
            var instance = new OverridingProxyBody(new ByteBufferBody(new byte[] { 0, 0, 0 }, 1, 2));
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
