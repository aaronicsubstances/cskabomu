using Kabomu.Common;
using Kabomu.Common.Bodies;
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
        private static IQuasiHttpTransport CreateTransportForBodyTransfer(object connection, int maxChunkSize,
            StringBuilder savedWrites, int maxWriteCount)
        {
            int writeCount = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                WriteBytesCallback = async (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    Assert.Equal(maxChunkSize, data.Length);
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

        [Theory]
        [MemberData(nameof(CreateTestReadBytesFullyData))]
        public async Task TestReadBytesFully(
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
                await TransportUtils.ReadBytesFully(body, data, offset, bytesToRead);
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
            expectedError = "end of quasi http body";
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
            int chunkSize, int maxWriteCount, string expectedError)
        {
            var tcs = new TaskCompletionSource<int>();
            var savedWrites = new StringBuilder();
            var transport = CreateTransportForBodyTransfer(connection, chunkSize, savedWrites, maxWriteCount);
            var bodyBytes = Encoding.UTF8.GetBytes(bodyData);
            var body = new ByteBufferBody(bodyBytes, 0, bodyBytes.Length, null);
            Exception actualException = null;
            try
            {
                await TransportUtils.TransferBodyToTransport(transport, connection, body, chunkSize);
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
            int chunkSize = 4;
            int maxWriteCount = 5;
            string expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = null;
            bodyData = "";
            chunkSize = 8;
            maxWriteCount = 0;
            expectedError = null;
            testData.Add(new object[] { connection, bodyData, chunkSize, maxWriteCount,
                expectedError });

            connection = 4;
            bodyData = "tintontannn!!!";
            chunkSize = 10;
            maxWriteCount = 3;
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
        public async Task TestReadBodyToEnd(IQuasiHttpBody body, int maxChunkSize, string expectedError, string expectedData)
        {
            byte[] data = null;
            Exception actualError = null;
            try
            {
                data = await TransportUtils.ReadBodyToEnd(body, maxChunkSize);
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

        [Fact]
        public async Task TestWriteEmpty()
        {
            // arrange.
            object connection = null;
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
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
            var transport = new ConfigurableQuasiHttpTransport
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

        [Fact]
        public async Task TestWriteByteSlicesForArgumentErrors()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return TransportUtils.WriteByteSlices(null, null, new ByteBufferSlice[0]);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return TransportUtils.WriteByteSlices(new ConfigurableQuasiHttpTransport(), null, null);
            });
            await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return TransportUtils.WriteByteSlices(new ConfigurableQuasiHttpTransport(), null, new ByteBufferSlice[] { null });
            });
        }
    }
}
