using Kabomu.Common;
using Kabomu.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class ByteOrientedTransferBodyTest
    {
        [Fact]
        public void TestEmptyReadWithContentLength()
        {
            // arrange.
            var dataList = new string[0];
            var transport = new TestNullTransport("lo", dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(null, transport, "lo", closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, null,
                new int[0], null, "");
            Assert.True(closed);
        }

        [Fact]
        public void TestEmptyReadWithoutContentLength()
        {
            // arrange.
            var dataList = new string[0];
            var transport = new TestNullTransport("lo", dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(null, transport, "lo", closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, null,
                new int[0], null, "");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyReadWithContentLength()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var transport = new TestNullTransport(null, dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody("text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyReadWithoutContentLength()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var transport = new TestNullTransport(null, dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody("text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestReadWithTransportError()
        {
            // arrange.
            var dataList = new string[] { "de", "al" };
            var transport = new TestNullTransport(1786, dataList, null, 0);
            var instance = new ByteOrientedTransferBody("image/gif", transport, 1786, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "image/gif",
                new int[] { 2, 2, 0 }, "END", null);
        }

        [Fact]
        public void TestReadWithTransportError2()
        {
            var transport = new TestConstantBehaviourTransport(null, -1, null);
            var instance = new ByteOrientedTransferBody(null, transport, null, null);
            var cbCalled = false;
            Action<Exception, int> cb = (e, len) =>
            {
                Assert.NotNull(e);
                Assert.Equal("invalid negative size received", e.Message);
                cbCalled = true;
            };
            instance.OnDataRead(new TestEventLoopApi(), new byte[2], 0, 1, cb);
            Assert.True(cbCalled);
        }

        [Fact]
        public void TestReadWithTransportError3()
        {
            var transport = new TestConstantBehaviourTransport(null, 100, null);
            var instance = new ByteOrientedTransferBody(null, transport, null, null);
            var cbCalled = false;
            Action<Exception, int> cb = (e, len) =>
            {
                Assert.NotNull(e);
                Assert.Equal("received bytes more than requested size", e.Message);
                cbCalled = true;
            };
            instance.OnDataRead(new TestEventLoopApi(), new byte[2], 0, 1, cb);
            Assert.True(cbCalled);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ByteOrientedTransferBody(null, null, null, () => { });
            });
            var instance = new ByteOrientedTransferBody(null, 
                new TestNullTransport(null, new string[0], null, 0), null, () => { });
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
