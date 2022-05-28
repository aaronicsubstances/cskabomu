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
            var closed = false;
            Action closeCb = () => closed = true;
            instance = new ChunkTransferBody(-1, "text/xml", readCallback, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }
    }
}
