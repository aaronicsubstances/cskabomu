using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
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
            var transport = new NullTransport(connection, new string[] { "car", "e" }, null, 0);
            byte[] data = new byte[4];
            int offset = 0;
            int bytesToRead = data.Length;
            string expectedError = null;
            string expectedData = "care";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = null;
            transport = new NullTransport(connection, new string[] { "are" }, null, 0);
            data = new byte[4];
            offset = 1;
            bytesToRead = 3;
            expectedError = null;
            expectedData = "are";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new NullTransport(connection, new string[] { "sen", "der", "s" }, null, 0);
            data = new byte[10];
            offset = 2;
            bytesToRead = 7;
            expectedError = null;
            expectedData = "senders";
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new NullTransport(connection, new string[] { "123", "der", "." }, null, 0);
            data = new byte[10];
            offset = 2;
            bytesToRead = 8;
            expectedError = "end of read";
            expectedData = null;
            testData.Add(new object[] { transport, connection,
                data, offset, bytesToRead,
                expectedError, expectedData});

            connection = 5;
            transport = new NullTransport(connection, new string[0], null, 0);
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
            var transport = new NullTransport(connection, null, savedWrites, maxWriteCount);
            transport.MaxMessageOrChunkSize = chunkSize;
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
    }
}
