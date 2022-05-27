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
        public void TestEmptyRead()
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            ByteOrientedTransferBody instance = null;
            var dataList = new string[0];
            var transport = new NullTransport("lo", dataList, null, 0);
            bool completed = false;
            Exception completionError = null;
            Action<Exception> completionCallback = e =>
            {
                completed = true;
                completionError = e;
            };
            instance = new ByteOrientedTransferBody(-1, null, transport, "lo", completionCallback);

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
            ByteOrientedTransferBody instance = null;
            var dataList = new string[] { "car", " ", "seat" };
            var transport = new NullTransport(null, dataList, null, 0);
            bool completed = false;
            Exception completionError = null;
            Action<Exception> completionCallback = e =>
            {
                completed = true;
                completionError = e;
            };
            instance = new ByteOrientedTransferBody(8, "text/xml", transport, null, completionCallback);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, 8, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.Null(completionError);
            Assert.True(completed);
        }
    }
}
