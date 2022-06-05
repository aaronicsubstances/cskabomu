using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class TransportUtilsTest
    {
        private static IQuasiHttpTransport CreateTransportForReadBytesFully(object connection, string[] dataChunks)
        {
            int readIndex = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int nextBytesRead = 0;
                    Exception e = null;
                    if (readIndex < dataChunks.Length)
                    {
                        var nextReadChunk = Encoding.UTF8.GetBytes(dataChunks[readIndex++]);
                        nextBytesRead = nextReadChunk.Length;
                        Array.Copy(nextReadChunk, 0, data, offset, nextBytesRead);
                    }
                    else if (readIndex == dataChunks.Length)
                    {
                        readIndex++;
                    }
                    else
                    {
                        e = new Exception("END");
                    }
                    cb.Invoke(e, nextBytesRead);
                }
            };
            return transport;
        }

        private static IQuasiHttpTransport CreateTransportForWriteBytesFully(object connection, int maxChunkSize,
            MemoryStream savedWrites, int maxWriteCount)
        {
            int writeCount = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                MaxChunkSize = maxChunkSize,
                WriteBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    Assert.Equal(maxChunkSize, data.Length);
                    Exception e = null;
                    if (writeCount < maxWriteCount)
                    {
                        savedWrites.Write(data, offset, length);
                        writeCount++;
                    }
                    else
                    {
                        e = new Exception("END");
                    }
                    cb.Invoke(e);
                }
            };
            return transport;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadBytesFullyData))]
        public async Task TestReadBytesFully(
            string[] dataChunks, object connection,
            byte[] data, int offset, int bytesToRead,
            string expectedError)
        {
            var transport = CreateTransportForReadBytesFully(connection, dataChunks);
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
                var expectedData = string.Join("", dataChunks);
                Assert.Equal(expectedData, actualData);
            }
        }

        public static List<object[]> CreateTestReadBytesFullyData()
        {
            var testData = new List<object[]>();

            object connection = "tea";
            var dataChunks = new string[] { "car", "e" };
            byte[] data = new byte[4];
            int offset = 0;
            int bytesToRead = data.Length;
            string expectedError = null;
            testData.Add(new object[] { dataChunks, connection,
                data, offset, bytesToRead, expectedError });

            connection = null;
            dataChunks = new string[] { "are" };
            data = new byte[4];
            offset = 1;
            bytesToRead = 3;
            expectedError = null;
            testData.Add(new object[] { dataChunks, connection,
                data, offset, bytesToRead, expectedError });

            connection = 5;
            dataChunks = new string[] { "sen", "der", "s" };
            data = new byte[10];
            offset = 2;
            bytesToRead = 7;
            expectedError = null;
            testData.Add(new object[] { dataChunks, connection,
                data, offset, bytesToRead, expectedError });

            connection = 5;
            dataChunks = new string[] { "123", "der", "." };
            data = new byte[10];
            offset = 2;
            bytesToRead = 8;
            expectedError = "end of read";
            testData.Add(new object[] { dataChunks, connection,
                data, offset, bytesToRead, expectedError });

            connection = 5;
            dataChunks = new string[0];
            data = new byte[10];
            offset = 7;
            bytesToRead = 0;
            expectedError = null;
            testData.Add(new object[] { dataChunks, connection,
                data, offset, bytesToRead, expectedError });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestTransferBodyToTransportData))]
        public async Task TestTransferBodyToTransport(object connection, string bodyData,
            int chunkSize, int maxWriteCount, string expectedError, byte[] expectedSavedWrites)
        {
            var tcs = new TaskCompletionSource<int>();
            var savedWrites = new MemoryStream();
            var transport = CreateTransportForWriteBytesFully(connection, chunkSize, savedWrites, maxWriteCount);
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
                Assert.Equal(expectedSavedWrites, savedWrites.ToArray());
            }
        }

        public static List<object[]> CreateTestTransferBodyToTransportData()
        {
            var testData = new List<object[]>();

            object connection = "tea";
            string bodyData = "care";
            int chunkSize = 3;
            int maxWriteCount = 5;
            string expectedError = null;
            byte[] expectedSavedWrites = new byte[] { 0, 1, (byte)'c', 0, 1, (byte)'a',
                0, 1, (byte)'r', 0, 1, (byte)'e', 0, 0 };
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError, expectedSavedWrites });

            connection = null;
            bodyData = "";
            chunkSize = 8;
            maxWriteCount = 1;
            expectedError = null;
            expectedSavedWrites = new byte[] { 0, 0 };
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError, expectedSavedWrites });

            connection = 4;
            bodyData = "tintontannn!!!";
            chunkSize = 12;
            maxWriteCount = 3;
            expectedError = null;
            expectedSavedWrites = new byte[] { 0, 10, (byte)'t', (byte)'i', (byte)'n', (byte)'t',
                 (byte)'o', (byte)'n', (byte)'t', (byte)'a', (byte)'n', (byte)'n', 0, 4, 
                 (byte)'n', (byte)'!', (byte)'!', (byte)'!', 0, 0 };
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError, expectedSavedWrites });

            connection = null;
            bodyData = "!!!";
            chunkSize = 7;
            maxWriteCount = 1;
            expectedError = "END";
            expectedSavedWrites = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError, expectedSavedWrites });

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

        private class ErrorQuasiHttpBody : IQuasiHttpBody
        {
            private readonly string _errorMessage;

            public ErrorQuasiHttpBody(string errorMessage)
            {
                _errorMessage = errorMessage;
            }

            public string ContentType => throw new NotImplementedException();

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
