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
            var transport = new NullTransport("lo", dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(0, null, transport, "lo", closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 0, null,
                new int[0], null, "");
            Assert.True(closed);
        }

        [Fact]
        public void TestEmptyReadWithoutContentLength()
        {
            // arrange.
            var dataList = new string[0];
            var transport = new NullTransport("lo", dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(-1, null, transport, "lo", closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, null,
                new int[0], null, "");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyReadWithContentLength()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var transport = new NullTransport(null, dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(8, "text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 8, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyReadWithoutContentLength()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var transport = new NullTransport(null, dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(-1, "text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestReadWithInsufficientContentLength()
        {
            // arrange.
            var dataList = new string[] { "car" };
            var transport = new NullTransport("De", dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(5, "text/xml", transport, "De", closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 5, "text/xml",
                new int[] { 3 }, "content length not achieved", null);
            Assert.True(closed);
        }

        [Fact]
        public void TestReadWithContentLengthExceeded()
        {
            // arrange.
            var dataList = new string[] { "c", "a", "r" };
            var transport = new NullTransport(4, dataList, null, 0);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ByteOrientedTransferBody(2, "application/xml", transport, 4, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 2, "application/xml",
                new int[] { 1, 1 }, "content length exceeded", null);
            Assert.True(closed);
        }

        [Fact]
        public void TestReadWithTransportError()
        {
            // arrange.
            var dataList = new string[] { "de", "al" };
            var transport = new NullTransport(1786, dataList, null, 0);
            var instance = new ByteOrientedTransferBody(-1, "image/gif", transport, 1786, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, "image/gif",
                new int[] { 2, 2, 0 }, "END", null);
        }

        [Fact]
        public void TestReadWithTransportError2()
        {
            var transport = new SameBehaviourTransport(null, -1, null);
            var instance = new ByteOrientedTransferBody(-1, null, transport, null, null);
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
            var transport = new SameBehaviourTransport(null, 100, null);
            var instance = new ByteOrientedTransferBody(-1, null, transport, null, null);
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
                new ByteOrientedTransferBody(0, null, null, null, () => { });
            });
            var instance = new ByteOrientedTransferBody(0, null, 
                new NullTransport(null, new string[0], null, 0), null, () => { });
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
