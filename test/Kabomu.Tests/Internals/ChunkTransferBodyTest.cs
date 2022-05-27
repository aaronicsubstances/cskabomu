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
        public void TestEmptyRead()
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
            bool completed = false;
            Exception completionError = null;
            Action<Exception> completionCallback = e =>
            {
                completed = true;
                completionError = e;
            };
            instance = new ChunkTransferBody(-1, null, readCallback, completionCallback);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, -1, null,
                new int[0], null, "");
            Assert.Null(completionError);
            Assert.True(completed);
        }

        [Fact]
        public void TestNonEmptyRead()
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
            bool completed = false;
            Exception completionError = null;
            Action<Exception> completionCallback = e =>
            {
                completed = true;
                completionError = e;
            };
            instance = new ChunkTransferBody(8, "text/xml", readCallback, completionCallback);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 8, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.Null(completionError);
            Assert.True(completed);
        }
    }
}
