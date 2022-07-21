﻿using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class StreamBackedBodyTest
    {
        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var backingStream = new MemoryStream();
            var instance = new StreamBackedBody(backingStream, 0, "text/csv");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestNonEmptyReadWithoutContentLength()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, -1, null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "application/octet-stream",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestNonEmptyRead()
        {
            // arrange.
            var backingStream = new MemoryStream(new byte[] { (byte)'A', (byte)'b', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, 3, null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "application/octet-stream",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StreamBackedBody(null, -1, null);
            });
            var backingStream = new MemoryStream(new byte[] { (byte)'c', (byte)'2' });
            var instance = new StreamBackedBody(backingStream, -1, null);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
