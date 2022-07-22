using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class EndOfReadNotifyingBodyTest
    {
        [Fact]
        public async Task TestEmptyRead()
        {
            // arrange.
            var cbCalled = false;
            Func<Task> endOfReadCb = () =>
            {
                cbCalled = true;
                return Task.CompletedTask;
            };
            var instance = new EndOfReadNotifyingBody(new ByteBufferBody(new byte[0], 0, 0, "text/plain"),
                endOfReadCb);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/plain",
                new int[0], null, new byte[0]);
            Assert.True(cbCalled);
        }

        [Fact]
        public async Task TestNonEmptyRead()
        {
            // arrange.
            var cbCalled = false;
            Func<Task> endOfReadCb = () =>
            {
                cbCalled = true;
                return Task.CompletedTask;
            };
            var expectedData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new EndOfReadNotifyingBody(new ByteBufferBody(expectedData, 0, expectedData.Length, "application/octet-stream"),
                endOfReadCb);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "application/octet-stream",
                new int[] { 2, 1 }, null, expectedData);
            Assert.True(cbCalled);
        }

        [Fact]
        public Task TestNonEmptyReadWithoutEndOfReadCallback()
        {
            // arrange.
            var expectedData = new byte[] { (byte)'A', (byte)'b', (byte)'2' };
            var instance = new EndOfReadNotifyingBody(new ByteBufferBody(expectedData, 0, expectedData.Length, "form"),
                null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "form",
                new int[] { 2, 1 }, null, expectedData);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new EndOfReadNotifyingBody(null, () => Task.CompletedTask);
            });
            var instance = new EndOfReadNotifyingBody(new ByteBufferBody(new byte[] { 0, 0, 0 }, 1, 2, null),
                () => Task.CompletedTask);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
