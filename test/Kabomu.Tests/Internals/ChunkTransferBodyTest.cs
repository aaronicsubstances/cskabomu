using Kabomu.Common;
using Kabomu.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class ChunkTransferBodyTest
    {
        [Fact]
        public void TestEmptyReadWithContentLength()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ChunkTransferBody instance = null;
            var dataList = new string[] { "" };
            var readIndex = 0;
            Action<int> readCallback = bytesToRead =>
            {
                var str = dataList[readIndex++];
                var data = Encoding.UTF8.GetBytes(str);
                instance.OnDataWrite(mutex, data, 0, data.Length);
            };
            var closed = false;
            Action closeCb = () => closed = true;
            instance = new ChunkTransferBody(0, null, readCallback, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 0, null,
                new int[0], null, "");
            Assert.True(closed);
        }
        [Fact]
        public void TestEmptyReadWithoutContentLength()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ChunkTransferBody instance = null;
            var dataList = new string[] { "" };
            var readIndex = 0;
            Action<int> readCallback = bytesToRead =>
            {
                var str = dataList[readIndex++];
                var data = Encoding.UTF8.GetBytes(str);
                instance.OnDataWrite(mutex, data, 0, data.Length);
            };
            var closed = false;
            Action closeCb = () => closed = true;
            instance = new ChunkTransferBody(-1, null, readCallback, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, null,
                new int[0], null, "");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyReadWithContentLength()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ChunkTransferBody instance = null;
            var dataList = new string[] { "car", " ", "seat", "" };
            var readIndex = 0;
            Action<int> readCallback = bytesToRead =>
            {
                var str = dataList[readIndex++];
                var data = Encoding.UTF8.GetBytes(str);
                instance.OnDataWrite(mutex, data, 0, data.Length);
            };
            var closed = false;
            Action closeCb = () => closed = true;
            instance = new ChunkTransferBody(8, "text/xml", readCallback, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 8, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyReadWithoutContentLength()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ChunkTransferBody instance = null;
            var dataList = new string[] { "car", " ", "seat", "" };
            var readIndex = 0;
            Action<int> readCallback = bytesToRead =>
            {
                var str = dataList[readIndex++];
                var data = Encoding.UTF8.GetBytes(str);
                instance.OnDataWrite(mutex, data, 0, data.Length);
            };
            instance = new ChunkTransferBody(-1, "text/xml", readCallback, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
        }

        [Fact]
        public void TestReadWithInsufficientContentLength()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ChunkTransferBody instance = null;
            var dataList = new string[] { "car", "" };
            var readIndex = 0;
            Action<int> readCallback = bytesToRead =>
            {
                var str = dataList[readIndex++];
                var data = Encoding.UTF8.GetBytes(str);
                instance.OnDataWrite(mutex, data, 0, data.Length);
            };
            instance = new ChunkTransferBody(5, "text/xml", readCallback, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 5, "text/xml",
                new int[] { 3 }, "content length not achieved", null);
        }

        [Fact]
        public void TestReadWithContentLengthExceeded()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ChunkTransferBody instance = null;
            var dataList = new string[] { "c", "a", "r" };
            var readIndex = 0;
            Action<int> readCallback = bytesToRead =>
            {
                var str = dataList[readIndex++];
                var data = Encoding.UTF8.GetBytes(str);
                instance.OnDataWrite(mutex, data, 0, data.Length);
            };
            var closed = false;
            Action closeCb = () => closed = true;
            instance = new ChunkTransferBody(2, "application/xml", readCallback, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 2, "application/xml",
                new int[] { 1, 1 }, "content length exceeded", null);
            Assert.True(closed);
        }

        [Fact]
        public void TestUsageError()
        {
            var mutex = new TestEventLoopApi();
            var instance = new ChunkTransferBody(0, null, len => { }, () => { });
            var cbCalled = false;
            instance.OnDataRead(mutex, new byte[1], 0, 1, (e, len) =>
            {
                Assert.NotNull(e);
                Assert.Equal("received chunk response larger than pending chunk request size", e.Message);
                cbCalled = true;
            });
            instance.OnDataWrite(mutex, new byte[2], 0, 2);
            Assert.True(cbCalled);
        }

        [Fact]
        public void TestUsageError2()
        {
            var mutex = new TestEventLoopApi();
            var instance = new ChunkTransferBody(0, null, len => { }, () => { });
            bool cbCalled1 = false, cbCalled2 = false;
            instance.OnDataRead(mutex, new byte[1], 0, 1, (e, len) =>
            {
                Assert.NotNull(e);
                Assert.Equal("end of read", e.Message);
                cbCalled1 = true;
            });
            instance.OnDataRead(mutex, new byte[1], 0, 1, (e, len) =>
            {
                Assert.NotNull(e);
                Assert.Equal("pending read unresolved", e.Message);
                cbCalled2 = true;
            });
            Assert.True(cbCalled2);
            Assert.False(cbCalled1);
            instance.OnEndRead(mutex, null);
            Assert.True(cbCalled1);
        }

        [Fact]
        public void TestUsageError3()
        {
            var mutex = new TestEventLoopApi();
            var instance = new ChunkTransferBody(0, null, len => { }, () => { });
            instance.OnDataWrite(mutex, new byte[0], 0, 0);
            var cbCalled = false;
            instance.OnDataRead(mutex, new byte[2], 0, 2, (e, len) =>
            {
                Assert.NotNull(e);
                Assert.Equal("received chunk response for no pending chunk request", e.Message);
                cbCalled = true;
            });
            Assert.True(cbCalled);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkTransferBody(0, null, null, () => { });
            });
            var instance = new ChunkTransferBody(0, null, len => { }, () => { });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnDataWrite(null, new byte[] { 0, 0, 0 }, 1, 2);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnDataWrite(new TestEventLoopApi(), new byte[] { 0, 0 }, 1, 2);
            });
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
