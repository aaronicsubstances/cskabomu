using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
{
    public class TransportBackedBodyTest
    {
        private ConfigurableQuasiHttpTransport CreateTransport(object connection, string[] strings)
        {
            var endOfReadSeen = false;
            var readIndex = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = 0;
                    Exception e = null;
                    if (endOfReadSeen)
                    {
                        e = new Exception("END");
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
                    cb.Invoke(e, bytesRead);
                }
            };
            return transport;
        }

        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var connection = "wer";
            var dataList = new string[0];
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection);
            instance.ContentType = "text/csv";

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(0, instance, 0, "text/csv",
                new int[0], null, new byte[0]);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            object connection = null;
            var dataList = new string[] { "Ab", "2" };
            var transport = CreateTransport(connection, dataList);
            var instance = new TransportBackedBody(transport, connection);
            instance.ContentType = "text/plain";
            instance.ContentLength = -1;
            var cbCalled = false;
            instance.CloseCallback = () =>
            {
                Assert.False(cbCalled);
                cbCalled = true;
            };

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "text/plain",
                new int[] { 2, 1 }, null, Encoding.UTF8.GetBytes("Ab2"));
            Assert.True(cbCalled);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new TransportBackedBody(null, null);
            });
            var dataList = new string[] { "c", "2" };
            var transport = CreateTransport(null, dataList);
            var instance = new TransportBackedBody(transport, null);
            instance.ContentType = "text/plain";
            instance.ContentLength = 2;
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
