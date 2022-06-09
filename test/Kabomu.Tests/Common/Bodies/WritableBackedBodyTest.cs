using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class WritableBackedBodyTest
    {
        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var instance = new WritableBackedBody(null);
            var cbCalled = false;
            instance.WriteLastBytes(new TestEventLoopApi(), new byte[0], 0, 0, e =>
            {
                Assert.False(cbCalled);
                Assert.Null(e);
                cbCalled = true;
            });

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(0, instance, null,
                new int[0], null, new byte[0]);
            Assert.True(cbCalled);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var instance = new WritableBackedBody("text/csv");
            var cbCalls = new bool[2];
            instance.WriteBytes(new TestEventLoopApi(), new byte[] { (byte)'A', (byte)'b' }, 0, 2, e =>
            {
                Assert.False(cbCalls[0]);
                Assert.Null(e);
                cbCalls[0] = true;
            });
            instance.WriteLastBytes(new TestEventLoopApi(), new byte[] { (byte)'2' }, 0, 1, e =>
            {
                Assert.False(cbCalls[1]);
                Assert.Null(e);
                cbCalls[1] = true;
            });

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(2, instance, "text/csv",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
            Assert.True(cbCalls[0]);
            Assert.True(cbCalls[1]);
        }

        [Fact]
        public void TestNonEmptyRead2()
        {
            // arrange.
            var expectedData = Encoding.UTF8.GetBytes("car seat");
            var instance = new WritableBackedBody("text/xml");
            var cbCalls = new bool[expectedData.Length];
            for (int i = 0; i < expectedData.Length; i++)
            {
                var capturedIndex = i;
                if (i == expectedData.Length - 1)
                {
                    instance.WriteLastBytes(new TestEventLoopApi(), expectedData, capturedIndex, 1, e =>
                    {
                        Assert.False(cbCalls[capturedIndex]);
                        Assert.Null(e);
                        cbCalls[capturedIndex] = true;
                    });
                }
                else
                {
                    instance.WriteBytes(new TestEventLoopApi(), expectedData, capturedIndex, 1, e =>
                    {
                        Assert.False(cbCalls[capturedIndex]);
                        Assert.Null(e);
                        cbCalls[capturedIndex] = true;
                    });
                }
            }

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(1, instance, "text/xml",
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 }, null, expectedData);
            for (int i = 0; i < cbCalls.Length; i++)
            {
                Assert.True(cbCalls[i]);
            }
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            var instance = new WritableBackedBody(null);
            var mutex = new TestEventLoopApi();
            instance.WriteLastBytes(mutex, new byte[] { (byte)'c', (byte)'2' }, 0, 2, 
                e => { });
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
            instance = new WritableBackedBody(null);
            var cbCalls = new bool[6];
            instance.ReadBytes(mutex, new byte[4], 0, 4, (e, len) =>
            {
                Assert.False(cbCalls[0]);
                Assert.Null(e);
                Assert.Equal(2, len);
                cbCalls[0] = true;
            });
            instance.ReadBytes(mutex, new byte[4], 0, 4, (e, len) =>
            {
                Assert.False(cbCalls[1]);
                Assert.NotNull(e);
                Assert.Contains("outstanding read exists", e.Message);
                cbCalls[1] = true;
            });
            instance.WriteLastBytes(mutex, new byte[] { (byte)'c', (byte)'2' }, 0, 2, e =>
            {
                Assert.False(cbCalls[2]);
                Assert.Null(e);
                cbCalls[2] = true;
            });
            instance.WriteLastBytes(mutex, new byte[] { (byte)'c', (byte)'2' }, 0, 2, e =>
            {
                Assert.False(cbCalls[3]);
                Assert.NotNull(e);
                Assert.Contains("end of write", e.Message);
                cbCalls[3] = true;
            });
            instance.WriteBytes(mutex, new byte[] { (byte)'c', (byte)'2' }, 0, 2, e =>
            {
                Assert.False(cbCalls[4]);
                Assert.NotNull(e);
                Assert.Contains("end of write", e.Message);
                cbCalls[4] = true;
            });
            instance.ReadBytes(mutex, new byte[4], 0, 4, (e, len) =>
            {
                Assert.False(cbCalls[5]);
                Assert.Null(e);
                Assert.Equal(0, len);
                cbCalls[5] = true;
            });
            for (int i = 0; i < cbCalls.Length; i++)
            {
                Assert.True(cbCalls[i]);
            }
        }
    }
}
