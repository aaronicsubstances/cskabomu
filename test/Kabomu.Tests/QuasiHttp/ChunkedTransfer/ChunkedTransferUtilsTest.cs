using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.ChunkedTransfer
{
    public class ChunkedTransferUtilsTest
    {

        /*[Fact]
        public async Task TestWriteLeadChunk()
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
            var leadChunk = new LeadChunk();
            var leadChunkSlices = leadChunk.Serialize();
            var lengthOfEncodedChunkLength = 3;
            var expectedStreamContents = new byte[lengthOfEncodedChunkLength + leadChunkSlices[0].Length + leadChunkSlices[1].Length];
            expectedStreamContents[2] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            Array.Copy(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, expectedStreamContents,
                lengthOfEncodedChunkLength, leadChunkSlices[0].Length);
            Array.Copy(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, expectedStreamContents,
                lengthOfEncodedChunkLength + leadChunkSlices[0].Length, leadChunkSlices[1].Length);

            // act.
            await ChunkEncodingBody.WriteLeadChunk(transport, connection, leadChunk, 1000);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
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
        }*/
    }
}
