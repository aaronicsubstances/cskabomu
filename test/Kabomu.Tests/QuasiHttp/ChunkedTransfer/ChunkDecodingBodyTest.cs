using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.Tests.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.ChunkedTransfer
{
    public class ChunkDecodingBodyTest
    {
        private static ConfigurableQuasiHttpBody CreateWrappedBody(string contentType, string[] strings)
        {
            var inputStream = new MemoryStream();
            foreach (var s in strings)
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                var chunk = new SubsequentChunk
                {
                    Version = LeadChunk.Version01,
                    Data = bytes,
                    DataLength = bytes.Length
                };
                var serialized = chunk.Serialize();
                var serializedLength = serialized.Sum(x => x.Length);
                var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
                ByteUtils.SerializeUpToInt64BigEndian(serializedLength,
                    encodedLength, 0, encodedLength.Length);
                inputStream.Write(encodedLength);
                foreach (var item in serialized)
                {
                    inputStream.Write(item.Data, item.Offset, item.Length);
                }
            }

            // end with terminator empty chunk.
            var terminatorChunk = new byte[MiscUtils.LengthOfEncodedChunkLength + 2];
            terminatorChunk[MiscUtils.LengthOfEncodedChunkLength - 1] = 2;
            terminatorChunk[MiscUtils.LengthOfEncodedChunkLength] = LeadChunk.Version01;
            inputStream.Write(terminatorChunk);

            inputStream.Position = 0; // rewind position for reads.

            var endOfInputSeen = false;
            var body = new ConfigurableQuasiHttpBody
            {
                ContentType = contentType,
                ReadBytesCallback = async (data, offset, length) =>
                {
                    int bytesRead = 0;
                    if (endOfInputSeen)
                    {
                        throw new Exception("END");
                    }
                    else
                    {
                        bytesRead = inputStream.Read(data, offset, length);
                        if (bytesRead == 0)
                        {
                            endOfInputSeen = true;
                        }
                    }
                    return bytesRead;
                },
                EndReadCallback = () => Task.CompletedTask
            };
            return body;
        }

        [Fact]
        public async Task TestEmptyRead()
        {
            // arrange.
            var dataList = new string[0];
            var wrappedBody = CreateWrappedBody(null, dataList);
            var instance = new ChunkDecodingBody(wrappedBody, 100);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(0, instance, -1, null,
                new int[0], null, new byte[0]);
        }

        [Fact]
        public async Task TestNonEmptyRead()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var wrappedBody = CreateWrappedBody("text/xml", dataList);
            var instance = new ChunkDecodingBody(wrappedBody, 100);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(4, instance, -1, "text/xml",
                new int[] { 3, 1, 4 }, null, Encoding.UTF8.GetBytes("car seat"));
        }

        [Fact]
        public async Task TestNonEmptyRead2()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var wrappedBody = CreateWrappedBody("text/csv", dataList);
            var instance = new ChunkDecodingBody(wrappedBody, 100);

            // act and assert.
            await CommonBodyTestRunner.RunCommonBodyTest(1, instance, -1, "text/csv",
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 }, null, Encoding.UTF8.GetBytes("car seat"));
        }

        [Fact]
        public async Task TestReadWithTransportError()
        {
            // arrange.
            var dataList = new string[] { "de", "a" };
            int readIndex = 0;
            var wrappedBody = new ConfigurableQuasiHttpBody
            {
                ContentType = "image/gif",
                ReadBytesCallback = async (data, offset, length) =>
                {
                    int bytesRead = 0;
                    switch (readIndex)
                    {
                        case 0:
                            data[offset] = 0;
                            data[offset + 1] = 0;
                            data[offset + 2] = 4;
                            bytesRead = 3;
                            break;
                        case 1:
                            data[offset] = LeadChunk.Version01;
                            data[offset + 1] = 0;
                            data[offset + 2] = (byte)'d';
                            data[offset + 3] = (byte)'e';
                            bytesRead = 4;
                            break;
                        case 2:
                            data[offset] = 0;
                            data[offset + 1] = 0;
                            data[offset + 2] = 3;
                            bytesRead = 3;
                            break;
                        case 3:
                            data[offset] = LeadChunk.Version01;
                            data[offset + 1] = 0;
                            data[offset + 2] = (byte)'a';
                            bytesRead = 3;
                            break;
                        default:
                            throw new Exception("END");
                    }
                    readIndex++;
                    return bytesRead;
                }
            };
            var instance = new ChunkDecodingBody(wrappedBody, 100);

            // Can't use CommonBodyTestRunner because expected transport error will
            // be an inner exception inside a chunk decoding exception.

            // so manually run an almost identical copy.

            // act and assert.
            Assert.Equal(-1, instance.ContentLength);
            Assert.Equal("image/gif", instance.ContentType);

            var readAccumulator = new MemoryStream();
            var readBuffer = new byte[2];
            foreach (int expectedBytesRead in new int[] { 2, 1 })
            {
                int bytesRead = await instance.ReadBytes(readBuffer, 0, readBuffer.Length);
                Assert.Equal(expectedBytesRead, bytesRead);
                readAccumulator.Write(readBuffer, 0, bytesRead);
            }
            var e = await Assert.ThrowsAnyAsync<Exception>(() =>
            {
                return instance.ReadBytes(readBuffer, 0, readBuffer.Length);
            });
            Assert.NotNull(e.InnerException);
            Assert.Contains("END", e.InnerException.Message);
        }

        [Fact]
        public async Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new ChunkDecodingBody(null, 100);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkDecodingBody(new StringBody("meat"), -1);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkDecodingBody(new StringBody(""), 0);
            });
            var instance = new ChunkDecodingBody(CreateWrappedBody(null, new string[0]), 100);
            await CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }

        [Fact]
        public async Task TestReadSubsequentChunkForMaxChunkExceededError()
        {
            // test for specific errors.
            var destStream = new MemoryStream();
            int maxChunkSize = 40;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(1_000_000, encodedLength, 0, encodedLength.Length);
            destStream.Write(encodedLength);
            var instance = new ChunkDecodingBody(new ByteBufferBody(destStream.ToArray(), 0, (int)destStream.Length),
                maxChunkSize);

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await instance.ReadBytes(new byte[1], 0, 1);
            });
            Assert.Contains("body", decodingError.Message);
            Assert.Contains("exceed", decodingError.Message);
            Assert.Contains("chunk size", decodingError.Message);
        }

        [Fact]
        public async Task TestReadSubsequentChunkForInsuffcientDataForLengthError()
        {
            var destStream = new MemoryStream();
            int maxChunkSize = 40;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength - 1];
            destStream.Write(encodedLength);
            var instance = new ChunkDecodingBody(new ByteBufferBody(destStream.ToArray()),
                maxChunkSize);

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await instance.ReadBytes(new byte[1], 0, 1);
            });
            Assert.Contains("body", decodingError.Message);
            Assert.Contains("chunk length", decodingError.Message);
        }

        [Fact]
        public async Task TestReadSubsequentChunkForInsuffcientDataError()
        {
            var destStream = new MemoryStream();
            var maxChunkSize = 40;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            encodedLength[MiscUtils.LengthOfEncodedChunkLength - 1] = 77;
            destStream.Write(encodedLength);
            destStream.Write(new byte[76]);
            var instance = new ChunkDecodingBody(new ByteBufferBody(destStream.ToArray(), 0, (int)destStream.Length),
                maxChunkSize);

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await instance.ReadBytes(new byte[1], 0, 1);
            });
            Assert.Contains("body", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadSubsequentChunkForInvalidChunkError()
        {
            // arrange.
            var destStream = new MemoryStream();
            byte maxChunkSize = 10;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            encodedLength[MiscUtils.LengthOfEncodedChunkLength - 1] = maxChunkSize;
            destStream.Write(encodedLength);
            destStream.Write(new byte[maxChunkSize]); // invalid since version is not set.
            var instance = new ChunkDecodingBody(new ByteBufferBody(destStream.ToArray()),
                maxChunkSize);

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await instance.ReadBytes(new byte[1], 0, 1);
            });
            Assert.Contains("body", decodingError.Message);
            Assert.Contains("invalid chunk", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunk()
        {
            // arrange.
            object connection = "dk";
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = destStream.Read(data, offset, length);
                    return Task.FromResult(bytesRead);
                }
            };
            int maxChunkSize = 100;
            var expectedChunk = new LeadChunk
            {
                Version = LeadChunk.Version01
            };
            var leadChunkSlices = expectedChunk.Serialize();
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            encodedLength[MiscUtils.LengthOfEncodedChunkLength - 1] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            destStream.Write(encodedLength);
            destStream.Write(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, leadChunkSlices[0].Length);
            destStream.Write(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, leadChunkSlices[1].Length);

            destStream.Position = 0; // reset for reading.

            // act
            var actualChunk = await ChunkDecodingBody.ReadLeadChunk(transport, connection, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForLaxityInChunkSizeCheck()
        {
            // arrange.
            object connection = "dkt";
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = destStream.Read(data, offset, length);
                    return Task.FromResult(bytesRead);
                }
            };
            int maxChunkSize = 10; // definitely less than actual serialized value but ok once it is less than 64K
            var expectedChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                RequestTarget = "/abcdefghijklmop"
            };
            var leadChunkSlices = expectedChunk.Serialize();
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            encodedLength[MiscUtils.LengthOfEncodedChunkLength - 1] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            destStream.Write(encodedLength);
            destStream.Write(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, leadChunkSlices[0].Length);
            destStream.Write(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, leadChunkSlices[1].Length);

            destStream.Position = 0; // reset for reading.

            // act
            var actualChunk = await ChunkDecodingBody.ReadLeadChunk(transport, connection, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForMaxChunkExceededError()
        {
            object connection = "tree";
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = destStream.Read(data, offset, length);
                    return Task.FromResult(bytesRead);
                }
            };
            int maxChunkSize = 40;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(1_000_000, encodedLength, 0, encodedLength.Length);
            destStream.Write(encodedLength);

            destStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkDecodingBody.ReadLeadChunk(transport, connection, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("exceed", decodingError.Message);
            Assert.Contains("chunk size", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataForLengthError()
        {
            object connection = null;
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = destStream.Read(data, offset, length);
                    return Task.FromResult(bytesRead);
                }
            };
            int maxChunkSize = 40;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength - 1];
            destStream.Write(encodedLength);

            destStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkDecodingBody.ReadLeadChunk(transport, connection, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("chunk length", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataError()
        {
            object connection = "nice length but incomplete data";
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = destStream.Read(data, offset, length);
                    return Task.FromResult(bytesRead);
                }
            };
            int maxChunkSize = 40;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            encodedLength[MiscUtils.LengthOfEncodedChunkLength - 1] = 77;
            destStream.Write(encodedLength);
            destStream.Write(new byte[76]);

            destStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkDecodingBody.ReadLeadChunk(transport, connection, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInvalidChunkError()
        {
            // arrange.
            object connection = 10;
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = destStream.Read(data, offset, length);
                    return Task.FromResult(bytesRead);
                }
            };
            byte maxChunkSize = 100;
            var encodedLength = new byte[MiscUtils.LengthOfEncodedChunkLength];
            encodedLength[MiscUtils.LengthOfEncodedChunkLength - 1] = maxChunkSize;
            destStream.Write(encodedLength);
            destStream.Write(new byte[maxChunkSize]);

            destStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkDecodingBody.ReadLeadChunk(transport, connection, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("invalid chunk", decodingError.Message);
        }
    }
}
