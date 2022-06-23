using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Shared
{
    public class CommonBodyTestRunner
    {
        public static async Task RunCommonBodyTest(int maxByteRead, IQuasiHttpBody instance,
            long expectedContentLength, string expectedContentType, 
            int[] expectedByteReads, string expectedError, byte[] expectedSuccessData)
        {
            // arrange.
            // ensure mininum buffer size of 1, so that unexpected no-op
            // reads do not occur.
            var buffer = new byte[Math.Max(maxByteRead, 1)];

            // act and assert.
            Assert.Equal(expectedContentLength, instance.ContentLength);
            Assert.Equal(expectedContentType, instance.ContentType);

            var readAccumulator = new MemoryStream();
            foreach (int expectedBytesRead in expectedByteReads)
            {
                int bytesRead = await instance.ReadBytes(buffer, 0, buffer.Length);
                Assert.Equal(expectedBytesRead, bytesRead);
                readAccumulator.Write(buffer, 0, bytesRead);
            }

            if (expectedError != null)
            {
                var e = await Assert.ThrowsAnyAsync<Exception>(() =>
                {
                    return instance.ReadBytes(buffer, 0, buffer.Length);
                });
                Assert.Equal(expectedError, e.Message);
            }
            else
            {
                var bytesRead = await instance.ReadBytes(buffer, 0, buffer.Length);
                Assert.Equal(0, bytesRead);
                Assert.Equal(expectedSuccessData, readAccumulator.ToArray());

                await instance.EndRead(null);
                await instance.EndRead(new Exception("test"));

                var e = await Assert.ThrowsAnyAsync<Exception>(() =>
                {
                    return instance.ReadBytes(buffer, 0, buffer.Length);
                });
                Assert.Equal("end of read", e.Message);
            }
        }

        public static async Task RunCommonBodyTestForArgumentErrors(IQuasiHttpBody instance)
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return instance.ReadBytes(new byte[] { 0, 0 }, 1, 2);
            });
        }
    }
}
