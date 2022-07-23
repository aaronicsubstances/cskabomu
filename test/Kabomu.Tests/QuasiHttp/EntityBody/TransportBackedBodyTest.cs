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
        public async Task TestEmptyRead()
        {
            // arrange.
            var connection = "wer";
            var dataList = new string[0];
            var transport = CreateTransport(connection, dataList);
            var cbCalled = false;
            Func<Task> endOfReadCb = () =>
            {
                cbCalled = true;
                return Task.CompletedTask;
            };
            var instance = new TransportBackedBody(transport, connection, 0, endOfReadCb)
            {
                ContentType = "text/csv"
            };

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
            Assert.True(cbCalled);
        }

        [Fact]
        public Task TestEmptyReadWithExcessData()
        {
            // arrange.
            var connection = "wer";
            var dataList = new string[] { "3y3", "yoma" };
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, 0, null)
            {
                ContentType = "application/json"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "application/json",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public async Task TestNonEmptyRead()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab", "2" };
            var transport = CreateTransport(connection, dataList);
            var cbCalled = false;
            Func<Task> endOfReadCb = () =>
            {
                cbCalled = true;
                return Task.CompletedTask;
            };
            var instance = new TransportBackedBody(transport, connection, -1, endOfReadCb)
            {
                ContentType = "text/plain"
            };

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "text/plain",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
            Assert.True(cbCalled);
        }

        [Fact]
        public Task TestNonEmptyReadWithContentLength()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab", "2" };
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, 3, null)
            {
                ContentType = "text/plain"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 3, "text/plain",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
        }

        [Fact]
        public Task TestNonEmptyReadWithExcessData()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab", "2er", "rea" };
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, 5, null)
            {
                ContentType = "application/json"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(3, instance, 5, "application/json",
                new int[] { 2, 3 }, null, Encoding.UTF8.GetBytes("Ab2er"));
        }

        [Fact]
        public Task TestWithEmptyTransportWhichDoesNotCompleteReadsAfterSatisfyingContentLength()
        {
            // arrange.
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport();
            var instance = new TransportBackedBody(transport, "hn", 0, null);

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 0, null,
                new int[0], null, new byte[0]);
        }

        [Fact]
        public Task TestWithTransportWhichDoesNotCompleteReadsAfterSatisfyingContentLength()
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
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 5, null,
                new int[] { 2, 2, 1 }, null, Encoding.UTF8.GetBytes("AbcD2"));
        }

        [Fact]
        public Task TestWithEmptyTransportWhichCannotCompleteReads()
        {
            // arrange.
            var connection = "wer";
            var dataList = new string[0];
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, 1, null)
            {
                ContentType = "text/csv"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(0, instance, 1, "text/csv",
                new int[0], "before end of read", null);
        }

        [Fact]
        public Task TestWithTransportWhichCannotCompleteReads()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab" };
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection, 5, null)
            {
                ContentType = "text/plain"
            };

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, 5, "text/plain",
                new int[] { 2 }, "before end of read", null);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new TransportBackedBody(null, null, 0, () => Task.CompletedTask);
            });
            var dataList = new string[] { "c", "2" };
            var transport = CreateTransport(null, dataList);
            var instance = new TransportBackedBody(transport, null, 2, null)
            {
                ContentType = "text/plain"
            };
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
