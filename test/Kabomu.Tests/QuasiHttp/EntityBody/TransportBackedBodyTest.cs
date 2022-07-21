using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.EntityBody
{
    public class TransportBackedBodyTest
    {
        private IQuasiHttpTransport CreateTransport(object connection, string[] strings)
        {
            var endOfReadSeen = false;
            var readIndex = 0;
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = 0;
                    if (endOfReadSeen)
                    {
                        throw new Exception("END");
                    }
                    else
                    {
                        if (readIndex >= strings.Length)
                        {
                            endOfReadSeen = true;
                        }
                        else
                        {
                            var nextBytes = Encoding.UTF8.GetBytes(strings[readIndex++]);
                            Array.Copy(nextBytes, 0, data, offset, nextBytes.Length);
                            bytesRead = nextBytes.Length;
                        }
                    }
                    return bytesRead;
                }
            };
            return transport;
        }

        [Fact]
        public Task TestEmptyRead()
        {
            // arrange.
            var connection = "wer";
            var dataList = new string[0];
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, 0, "text/csv");

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public async Task TestNonEmptyRead()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab", "2" };
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, -1, "text/plain");

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "text/plain",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public async Task TestWithEmptyTransportWhichDoesNotCompleteReadsAfterSatisfyingContentLength()
        {
            // arrange.
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport();
            var instance = new TransportBackedBody(transport, "hn", 0, null);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(2, instance, 0, null,
                new int[0], null, new byte[0]);
        }

        [Fact]
        public async Task TestWithTransportWhichDoesNotCompleteReadsAfterSatisfyingContentLength()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab", "cD", "2" };
            var readIndex = 0;
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Null(actualConnection);
                    if (readIndex >= dataList.Length)
                    {
                        // return a task which will never be completed.
                        return new TaskCompletionSource<int>().Task;
                    }
                    var srcBytes = Encoding.UTF8.GetBytes(dataList[readIndex++]);
                    Array.Copy(srcBytes, 0, data, offset, srcBytes.Length);
                    return Task.FromResult(srcBytes.Length);
                }
            };
            var instance = new TransportBackedBody(transport, connection, 5, null);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(2, instance, 5, null,
                new int[] { 2, 2, 1 }, null, Encoding.UTF8.GetBytes("AbcD2"));
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new TransportBackedBody(null, null, 0, null);
            });
            var dataList = new string[] { "c", "2" };
            var transport = CreateTransport(null, dataList);
            var instance = new TransportBackedBody(transport, null, 2, "text/plain");
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
