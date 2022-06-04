using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class CommonBodyTestRunner
    {
        public static void RunCommonBodyTest(IQuasiHttpBody instance,
            string expectedContentType, int[] expectedByteReads, string expectedError,
            string expectedSuccessData)
        {
            // arrange.
            var mutex = new TestEventLoopApi();
            int maxByteRead = 0;
            if (expectedByteReads.Length > 0)
            {
                maxByteRead = expectedByteReads.Max();
            }
            var buffer = new byte[maxByteRead];

            // act and assert.
            Assert.Equal(expectedContentType, instance.ContentType);

            var readAccumulator = new StringBuilder();
            foreach (int expectedBytesRead in expectedByteReads)
            {
                Action<Exception, int> cb = (e, bytesRead) =>
                {
                    Assert.Null(e);
                    Assert.Equal(expectedBytesRead, bytesRead);
                    for (int i = 0; i < bytesRead; i++)
                    {
                        readAccumulator.Append((char)buffer[i]);
                    }
                };
                instance.OnDataRead(mutex, buffer, 0, buffer.Length, cb);
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
                instance.OnDataRead(mutex, buffer, 0, buffer.Length, cb2);
                Assert.True(cbCalled);
            }
            else
            {
                var cbCalled = false;
                Action<Exception, int> cb2 = (e, bytesRead) =>
                {
                    Assert.Null(expectedError);
                    Assert.Equal(0, bytesRead);
                    cbCalled = true;
                };
                instance.OnDataRead(mutex, buffer, 0, buffer.Length, cb2);
                Assert.True(cbCalled);
                Assert.Equal(expectedSuccessData, readAccumulator.ToString());

                instance.OnEndRead(mutex, null);
                instance.OnEndRead(mutex, new Exception("test"));

                cbCalled = false;
                Action<Exception, int> endCb = (e, bytesRead) =>
                {
                    Assert.NotNull(e);
                    Assert.Equal("end of read", e.Message);
                    cbCalled = true;
                };
                instance.OnDataRead(mutex, buffer, 0, buffer.Length, endCb);
                Assert.True(cbCalled);
            }
        }

        public static void RunCommonBodyTestForArgumentErrors(IQuasiHttpBody instance)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnDataRead(null, new byte[] { 0, 0, 0 }, 1, 2, (e, len) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnEndRead(null, null);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnDataRead(new TestEventLoopApi(), new byte[] { 0, 0 }, 1, 2, (e, len) => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                instance.OnDataRead(new TestEventLoopApi(), new byte[] { 0, 0, 0 }, 1, 2, null);
            });
        }
    }
}
