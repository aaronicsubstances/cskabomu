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
    }
}
