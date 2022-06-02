using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class TransportUtilsTest
    {
        [Theory]
        [MemberData(nameof(CreateTestReadBytesFullyData))]
        public async Task TestReadBytesFully(
            IQuasiHttpTransport transport, object connection,
            byte[] data, int offset, int bytesToRead,
            string expectedError, string expectedData)
        {
            var tcs = new TaskCompletionSource<int>();
            TransportUtils.ReadBytesFully(transport, connection, data, offset, bytesToRead,
                e =>
                {
                    if (e != null)
                    {
                        tcs.SetException(e);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                });
            Exception actualException = null;
            try
            {
                await tcs.Task;
            }
            catch (Exception e)
            {
                actualException = e;
            }
            if (expectedError != null)
            {
                Assert.NotNull(actualException);
                Assert.Equal(expectedError, actualException.Message);
            }
            else
            {
                Assert.Null(actualException);
                string actualData = Encoding.UTF8.GetString(data, offset, bytesToRead);
                Assert.Equal(expectedData, actualData);
            }
        }

        public static List<object[]> CreateTestReadBytesFullyData()
        {
            var testData = new List<object[]>();

            object connection = "tea";
            var transport = new TestNullTransport(connection, new string[] { "car", "e" }, null, 0);
            byte[] data = new byte[4];
            int offset = 0;
            int bytesToRead = data.Length;
            string expectedError = null;
            string expectedData = "care";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = null;
            transport = new TestNullTransport(connection, new string[] { "are" }, null, 0);
            data = new byte[4];
            offset = 1;
            bytesToRead = 3;
            expectedError = null;
            expectedData = "are";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new TestNullTransport(connection, new string[] { "sen", "der", "s" }, null, 0);
            data = new byte[10];
            offset = 2;
            bytesToRead = 7;
            expectedError = null;
            expectedData = "senders";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new TestNullTransport(connection, new string[] { "123", "der", "." }, null, 0);
            data = new byte[10];
            offset = 2;
            bytesToRead = 8;
            expectedError = "end of read";
            expectedData = null;
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new TestNullTransport(connection, new string[0], null, 0);
            data = new byte[10];
            offset = 7;
            bytesToRead = 0;
            expectedError = null;
            expectedData = "";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestTransferBodyToTransportData))]
        public async Task TestTransferBodyToTransport(object connection, string bodyData,
            int chunkSize, int maxWriteCount, string expectedError)
        {
            var tcs = new TaskCompletionSource<int>();
            var savedWrites = new StringBuilder();
            var transport = new TestNullTransport(connection, null, savedWrites, maxWriteCount);
            transport.MaxChunkSize = chunkSize;
            var bodyBytes = Encoding.UTF8.GetBytes(bodyData);
            var body = new ByteBufferBody(bodyBytes, 0, bodyBytes.Length, null);
            TransportUtils.TransferBodyToTransport(transport, connection, body, new TestEventLoopApi(),
                e =>
                {
                    if (e != null)
                    {
                        tcs.SetException(e);
                    }
                    else
                    {
                        tcs.SetResult(0);
                    }
                });
            Exception actualException = null;
            try
            {
                await tcs.Task;
            }
            catch (Exception e)
            {
                actualException = e;
            }
            if (expectedError != null)
            {
                Assert.NotNull(actualException);
                Assert.Equal(expectedError, actualException.Message);
            }
            else
            {
                Assert.Null(actualException);
                Assert.Equal(bodyData, savedWrites.ToString());
            }
        }

        public static List<object[]> CreateTestTransferBodyToTransportData()
        {
            var testData = new List<object[]>();

            object connection = "tea";
            string bodyData = "care";
            int chunkSize = 1;
            int maxWriteCount = 4;
            string expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "";
            chunkSize = 0;
            maxWriteCount = 0;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = 4;
            bodyData = "tintontannn!!!";
            chunkSize = 10;
            maxWriteCount = 2;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "!!!";
            chunkSize = 2;
            maxWriteCount = 1;
            expectedError = "END";
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadBodyToEndData))]
        public void TestReadBodyToEnd(IQuasiHttpBody body, int maxChunkSize, string expectedError, string expectedData)
        {
            var cbCalled = false;
            TransportUtils.ReadBodyToEnd(body, new TestEventLoopApi(), maxChunkSize, (e, data) =>
            {
                Assert.False(cbCalled);
                if (expectedError != null)
                {
                    Assert.NotNull(e);
                    Assert.Equal(expectedError, e.Message);
                }
                else
                {
                    Assert.Null(e);
                    var actualData = Encoding.UTF8.GetString(data);
                    Assert.Equal(expectedData, actualData);
                    Exception eofError = null;
                    body.OnDataRead(new TestEventLoopApi(), new byte[1], 0, 1, (e, i) =>
                    {
                        eofError = e;
                    });
                    Assert.NotNull(eofError);
                    Assert.Equal("end of read", eofError.Message);
                }
                cbCalled = true;
            });
            Assert.True(cbCalled);
        }

        public static List<object[]> CreateTestReadBodyToEndData()
        {
            var testData = new List<object[]>();

            var data = "abcdefghijklmnopqrstuvwxyz";
            IQuasiHttpBody body = new StringBody(data, null);
            int maxChunkSize = 5;
            string expectedError = null;
            testData.Add(new object[] { body, maxChunkSize, expectedError, data });

            data = "0123456789";
            var dataBytes = Encoding.UTF8.GetBytes(data);
            body = new ByteBufferBody(dataBytes, 0, dataBytes.Length, null);
            maxChunkSize = 1;
            expectedError = null;
            testData.Add(new object[] { body, maxChunkSize, expectedError, data });

            data = null;
            expectedError = "test read error";
            body = new ErrorQuasiHttpBody(expectedError);
            maxChunkSize = 10;
            testData.Add(new object[] { body, maxChunkSize, expectedError, data });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestIsRequestPduData))]
        public void TestIsRequestPdu(byte[] data, int offset, int length, bool expected)
        {
            var actual = TransportUtils.IsRequestPdu(data, offset, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestIsRequestPduData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 0, 1, 3, 1 }, 1, 3, true },
                new object[]{ new byte[] { 1, 2 }, 0, 2, false },
            };
        }

        [Theory]
        [MemberData(nameof(CreateTestGetPduSequenceNumberData))]
        public void TestGetPduSequenceNumber(byte[] data, int offset, int length, int expected)
        {
            var actual = TransportUtils.GetPduSequenceNumber(data, offset, length);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestGetPduSequenceNumberData()
        {
            return new List<object[]>
            {
                new object[]{ new byte[] { 1, 1, 0, 1, 2, 3, 4 }, 0, 7, 16_909_060 },
                new object[]{ new byte[] { 0, 1, 5, 0, 1, 1, 0, 0, 2, 3, 7 }, 1, 10, 16_842_752 },
            };
        }

        private class ErrorQuasiHttpBody : IQuasiHttpBody
        {
            private readonly string _errorMessage;

            public ErrorQuasiHttpBody(string errorMessage)
            {
                _errorMessage = errorMessage;
            }

            public string ContentType => throw new NotImplementedException();

            public int ContentLength => throw new NotImplementedException();

            public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
            {
                cb.Invoke(new Exception(_errorMessage), 0);
            }

            public void OnEndRead(IMutexApi mutex, Exception e)
            {
                throw new NotImplementedException();
            }
        }
    }
}
