using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class CommonBodyTestRunner
    {
        public static void RunCommonBodyTest(int maxByteRead, IQuasiHttpBody instance,
            string expectedContentType, int[] expectedByteReads, string expectedError,
            byte[] expectedSuccessData)
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            // ensure mininum buffer size of 1, so that unexpected no-op
            // reads do not occur.
            var buffer = new byte[Math.Max(maxByteRead, 1)];

            // act and assert.
            Assert.Equal(expectedContentType, instance.ContentType);

            var readAccumulator = new MemoryStream();
            foreach (int expectedBytesRead in expectedByteReads)
            {
                Action<Exception, int> cb = (e, bytesRead) =>
                {
                    Assert.Null(e);
                    Assert.Equal(expectedBytesRead, bytesRead);
                    readAccumulator.Write(buffer, 0, bytesRead);
                };
                instance.ReadBytes(mutex, buffer, 0, buffer.Length, cb);
            }

            if (expectedError != null)
            {
                var cbCalled = false;
                Action<Exception, int> cb2 = (e, bytesRead) =>
                {
                    Assert.NotNull(e);
                    Assert.Equal(expectedError, e.Message);
                    cbCalled = true;
                };
                instance.ReadBytes(mutex, buffer, 0, buffer.Length, cb2);
                Assert.True(cbCalled);
            }
            else
            {
                var cbCalled = false;
                Action<Exception, int> cb2 = (e, bytesRead) =>
                {
                    Assert.Null(e);
                    Assert.Equal(0, bytesRead);
                    cbCalled = true;
                };
                instance.ReadBytes(mutex, buffer, 0, buffer.Length, cb2);
                Assert.True(cbCalled);
                Assert.Equal(expectedSuccessData, readAccumulator.ToArray());

                instance.OnEndRead(mutex, null);
                instance.OnEndRead(mutex, new Exception("test"));

                cbCalled = false;
                Action<Exception, int> endCb = (e, bytesRead) =>
                {
                    Assert.NotNull(e);
                    Assert.Equal("end of read", e.Message);
                    cbCalled = true;
                };
                instance.ReadBytes(mutex, buffer, 0, buffer.Length, endCb);
                Assert.True(cbCalled);
            }
        }

        public static void RunCommonBodyTestForArgumentErrors(IQuasiHttpBody instance)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ReadBytes(null, new byte[] { 0, 0, 0 }, 1, 2, (e, len) => { });
                if (instance != null)
                {
                    int a = 1 + 3;
                }
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnEndRead(null, null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ReadBytes(new TestEventLoopApi(), new byte[] { 0, 0 }, 1, 2, (e, len) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.ReadBytes(new TestEventLoopApi(), new byte[] { 0, 0, 0 }, 1, 2, null);
            });
        }
    }
}
