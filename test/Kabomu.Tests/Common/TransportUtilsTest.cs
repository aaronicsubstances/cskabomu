using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
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
        private static IQuasiHttpTransport CreateTransportForBodyTransfer(object connection, int bufferSize,
            StringBuilder savedWrites, int maxWriteCount)
        {
            int writeCount = 0;
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                WriteBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    Assert.Equal(bufferSize, data.Length);
                    if (writeCount < maxWriteCount)
                    {
                        savedWrites.Append(Encoding.UTF8.GetString(data, offset, length));
                        writeCount++;
                    }
                    else
                    {
                        throw new Exception("END");
                    }
                }
            };
            return transport;
        }

        private static IQuasiHttpTransport CreateTransportForDirectTransfer(object expectedConnection, string data, int bufferSize)
        {
            var srcData = Encoding.UTF8.GetBytes(data);
            int srcDataOffset = 0;
            bool connectionDisposed = false;
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(expectedConnection, actualConnection);
                    Assert.True(bufferSize >= length);
                    if (connectionDisposed)
                    {
                        throw new Exception("connection disposed");
                    }
                    var lengthToUse = Math.Min(srcData.Length - srcDataOffset, length);
                    Array.Copy(srcData, srcDataOffset, data, offset, lengthToUse);
                    srcDataOffset += lengthToUse;
                    return lengthToUse;
                },
                ReleaseConnectionCallback = async (actualConnection) =>
                {
                    connectionDisposed = true;
                }
            };
            return transport;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadBodyBytesFullyData))]
        public async Task TestReadBodyBytesFully(
            string[] dataChunks,
            byte[] data, int offset, int bytesToRead,
            string expectedError)
        {
            var readIndex = 0;
            var body = new ConfigurableQuasiHttpBody
            {
                ReadBytesCallback = async (data, offset, length) =>
                {
                    int nextBytesRead = 0;
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
                        throw new Exception("END");
                    }
                    return nextBytesRead;
                }
            };
            Exception actualException = null;
            try
            {
                await TransportUtils.ReadBodyBytesFully(body, data, offset, bytesToRead);
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

        public static List<object[]> CreateTestReadBodyBytesFullyData()
        {
            var testData = new List<object[]>();

            var dataChunks = new string[] { "car", "e" };
            byte[] data = new byte[4];
            int offset = 0;
            int bytesToRead = data.Length;
            string expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[] { "are" };
            data = new byte[4];
            offset = 1;
            bytesToRead = 3;
            expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[] { "sen", "der", "s" };
            data = new byte[10];
            offset = 2;
            bytesToRead = 7;
            expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[] { "123", "der", "." };
            data = new byte[10];
            offset = 2;
            bytesToRead = 8;
            expectedError = "unexpected end of read";
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[0];
            data = new byte[10];
            offset = 7;
            bytesToRead = 0;
            expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadTransportBytesFullyData))]
        public async Task TestReadTransportBytesFully(
            string[] dataChunks,
            byte[] data, int offset, int bytesToRead,
            string expectedError)
        {
            var readIndex = 0;
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(bytesToRead, actualConnection);
                    int nextBytesRead = 0;
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
                        throw new Exception("END");
                    }
                    return nextBytesRead;
                }
            };
            Exception actualException = null;
            try
            {
                await TransportUtils.ReadTransportBytesFully(transport, bytesToRead, data, offset, bytesToRead);
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

        public static List<object[]> CreateTestReadTransportBytesFullyData()
        {
            var testData = new List<object[]>();

            var dataChunks = new string[] { "car", "e" };
            byte[] data = new byte[4];
            int offset = 0;
            int bytesToRead = data.Length;
            string expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[] { "are" };
            data = new byte[4];
            offset = 1;
            bytesToRead = 3;
            expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[] { "sen", "der", "s" };
            data = new byte[10];
            offset = 2;
            bytesToRead = 7;
            expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[] { "123", "der", "." };
            data = new byte[10];
            offset = 2;
            bytesToRead = 8;
            expectedError = "unexpected end of read";
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            dataChunks = new string[0];
            data = new byte[10];
            offset = 7;
            bytesToRead = 0;
            expectedError = null;
            testData.Add(new object[] { dataChunks,
                data, offset, bytesToRead, expectedError });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestTransferBodyToTransportData))]
        public async Task TestTransferBodyToTransport(object connection, string bodyData,
            int bufferSize, int maxWriteCount, string expectedError)
        {
            var savedWrites = new StringBuilder();
            var transport = CreateTransportForBodyTransfer(connection, bufferSize, savedWrites, maxWriteCount);
            var bodyBytes = Encoding.UTF8.GetBytes(bodyData);
            var body = new ByteBufferBody(bodyBytes, 0, bodyBytes.Length);
            Exception actualException = null;
            try
            {
                await TransportUtils.TransferBodyToTransport(transport, connection, body, bufferSize);
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
            int bufferSize = 4;
            int maxWriteCount = 5;
            string expectedError = null;
            testData.Add(new object[] { connection, bodyData, bufferSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "";
            bufferSize = 8;
            maxWriteCount = 0;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, bufferSize, maxWriteCount,
                expectedError });

            connection = 4;
            bodyData = "tintontannn!!!";
            bufferSize = 10;
            maxWriteCount = 3;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, bufferSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "!!!";
            bufferSize = 2;
            maxWriteCount = 1;
            expectedError = "END";
            testData.Add(new object[] { connection, bodyData, bufferSize, maxWriteCount,
                expectedError });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadBodyToEndData))]
        public async Task TestReadBodyToEnd(IQuasiHttpBody body, int bufferSize, string expectedError, string expectedData)
        {
            byte[] data = null;
            Exception actualError = null;
            try
            {
                data = await TransportUtils.ReadBodyToEnd(body, bufferSize);
            }
            catch (Exception e)
            {
                actualError = e;
            }
            if (expectedError == null)
            {
                Assert.Null(actualError);
                var actualData = Encoding.UTF8.GetString(data);
                Assert.Equal(expectedData, actualData);
                Exception eofError = await Assert.ThrowsAnyAsync<Exception>(() =>
                    body.ReadBytes(new byte[1], 0, 1));
                Assert.Equal("end of read", eofError.Message);
            }
            else
            {
                Assert.NotNull(actualError);
                Assert.Equal(expectedError, actualError.Message);
            }
        }

        public static List<object[]> CreateTestReadBodyToEndData()
        {
            var testData = new List<object[]>();

            var data = "abcdefghijklmnopqrstuvwxyz";
            IQuasiHttpBody body = new StringBody(data);
            int maxChunkSize = 5;
            string expectedError = null;
            testData.Add(new object[] { body, maxChunkSize, expectedError, data });

            data = "0123456789";
            var dataBytes = Encoding.UTF8.GetBytes(data);
            body = new ByteBufferBody(dataBytes, 0, dataBytes.Length);
            maxChunkSize = 1;
            expectedError = null;
            testData.Add(new object[] { body, maxChunkSize, expectedError, data });

            data = null;
            expectedError = "test read error";
            var capturedError = expectedError;
            body = new ConfigurableQuasiHttpBody
            {
                ReadBytesCallback = (data, offset, length) =>
                {
                    throw new Exception(capturedError);
                }
            };
            maxChunkSize = 10;
            testData.Add(new object[] { body, maxChunkSize, expectedError, data });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestReadBodyToMemoryStreamData))]
        public async Task TestReadBodyToMemoryStream(IQuasiHttpBody body, int bufferSize, int bufferingLimit,
            string expectedError, string expectedData)
        {
            Stream stream = null;
            Exception actualError = null;
            try
            {
                stream = await TransportUtils.ReadBodyToMemoryStream(body, bufferSize, bufferingLimit);
            }
            catch (Exception e)
            {
                actualError = e;
            }
            if (expectedError == null)
            {
                Assert.Null(actualError);
                var data = await TransportUtils.ReadBodyToEnd(new StreamBackedBody(stream, -1), 100);
                var actualData = Encoding.UTF8.GetString(data);
                Assert.Equal(expectedData, actualData);
                Exception eofError = await Assert.ThrowsAnyAsync<Exception>(() =>
                    body.ReadBytes(new byte[1], 0, 1));
                Assert.Equal("end of read", eofError.Message);
            }
            else
            {
                Assert.NotNull(actualError);
                Assert.Contains(expectedError, actualError.Message);
            }
        }

        public static List<object[]> CreateTestReadBodyToMemoryStreamData()
        {
            var testData = new List<object[]>();

            var data = "abcdefghijklmnopqrstuvwxyz";
            IQuasiHttpBody body = new StringBody(data);
            int bufferSize = 5;
            int bufferingLimit = -1;
            string expectedError = null;
            testData.Add(new object[] { body, bufferSize, bufferingLimit, expectedError, data });

            data = "0123456789";
            var dataBytes = Encoding.UTF8.GetBytes(data);
            body = new ByteBufferBody(dataBytes, 0, dataBytes.Length);
            bufferSize = 1;
            bufferingLimit = 10;
            expectedError = null;
            testData.Add(new object[] { body, bufferSize, bufferingLimit, expectedError, data });

            data = "0123456789";
            dataBytes = Encoding.UTF8.GetBytes(data);
            body = new ByteBufferBody(dataBytes, 0, dataBytes.Length);
            bufferSize = 1;
            bufferingLimit = 9;
            expectedError = "limit of 9";
            testData.Add(new object[] { body, bufferSize, bufferingLimit, expectedError, null });

            data = "abcdefghijklmnopqrstuvwxyz";
            body = new StringBody(data);
            bufferSize = 10;
            bufferingLimit = 20;
            expectedError = "limit of 20";
            testData.Add(new object[] { body, bufferSize, bufferingLimit, expectedError, null });

            return testData;
        }

        [Fact]
        public async Task TestWriteEmpty()
        {
            // arrange.
            object connection = null;
            var destStream = new MemoryStream();
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                WriteBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    destStream.Write(data, offset, length);
                    return Task.CompletedTask;
                }
            };
            var slices = new ByteBufferSlice[0];
            var expectedStreamContents = new byte[0];

            // act.
            await TransportUtils.WriteByteSlices(transport, connection, slices);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteByteSlices()
        {
            // arrange.
            object connection = "dk";
            var destStream = new MemoryStream();
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                WriteBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    destStream.Write(data, offset, length);
                    return Task.CompletedTask;
                }
            };
            var slices = new ByteBufferSlice[]
            {
                new ByteBufferSlice
                {
                    Data = new byte[]{ 0 },
                    Length = 1
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 0, 2, 1 },
                    Offset = 1,
                    Length = 2
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 7, 8, 9, 10 },
                    Length = 3
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 7, 8, 9, 10 },
                    Length = 0
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 7, 8, 9, 10, 11 },
                    Offset = 3,
                    Length = 1
                }
            };
            var expectedStreamContents = new byte[] { 0, 2, 1, 7, 8, 9, 10 };

            // act.
            await TransportUtils.WriteByteSlices(transport, connection, slices);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }
    }
}
