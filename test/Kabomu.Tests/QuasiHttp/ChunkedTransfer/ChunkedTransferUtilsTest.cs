using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
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
        [Theory]
        [MemberData(nameof(CreateTestEncodeSubsequentChunkHeaderData))]
        public void TestEncodeSubsequentChunkHeader(int chunkDataLength,
            int dataLength, int offset, byte[] expected)
        {
            var data = new byte[dataLength];
            ChunkedTransferUtils.EncodeSubsequentChunkHeader(
                chunkDataLength, data, offset);
            ComparisonUtils.CompareData(expected, 0, expected.Length,
                data, offset, 5);
        }

        public static List<object[]> CreateTestEncodeSubsequentChunkHeaderData()
        {
            var testData = new List<object[]>();

            int chunkDataLength = 0;
            int dataLength = 5;
            int offset = 0;
            var expected = new byte[] { 0, 0, 2, 1, 0 };
            testData.Add(new object[] { chunkDataLength, dataLength,
                offset, expected });

            chunkDataLength = 555;
            dataLength = 50;
            offset = 20;
            expected = new byte[] { 0, 2, 0x2d, 1, 0 };
            testData.Add(new object[] { chunkDataLength, dataLength,
                offset, expected });

            chunkDataLength = 511_665;
            dataLength = 10;
            offset = 2;
            expected = new byte[] { 7, 0xce, 0xb3, 1, 0 };
            testData.Add(new object[] { chunkDataLength, dataLength,
                offset, expected });

            return testData;
        }

        [Fact]
        public async Task TestWriteLeadChunk()
        {
            // arrange.
            var destStream = new MemoryStream();
            var writer = new StreamCustomReaderWriter(destStream);
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
            await ChunkedTransferUtils.WriteLeadChunk(writer, 1000, leadChunk);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestReadLeadChunk()
        {
            // arrange.
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            int maxChunkSize = 100;
            var expectedChunk = new LeadChunk
            {
                Version = LeadChunk.Version01
            };
            var leadChunkSlices = expectedChunk.Serialize();
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            encodedLength[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            srcStream.Write(encodedLength);
            srcStream.Write(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, leadChunkSlices[0].Length);
            srcStream.Write(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, leadChunkSlices[1].Length);

            srcStream.Position = 0; // reset for reading.

            // act
            var actualChunk = await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForLaxityInChunkSizeCheck()
        {
            // arrange.
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            int maxChunkSize = 10; // definitely less than actual serialized value but ok once it is less than 64K
            var expectedChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                RequestTarget = "/abcdefghijklmop"
            };
            var leadChunkSlices = expectedChunk.Serialize();
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            encodedLength[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            srcStream.Write(encodedLength);
            srcStream.Write(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, leadChunkSlices[0].Length);
            srcStream.Write(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, leadChunkSlices[1].Length);

            srcStream.Position = 0; // reset for reading.

            // act
            var actualChunk = await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForMaxChunkExceededError()
        {
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            int maxChunkSize = 40;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(1_000_000, encodedLength, 0, encodedLength.Length);
            srcStream.Write(encodedLength);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("exceed", decodingError.Message);
            Assert.Contains("chunk size", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataForLengthError()
        {
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            int maxChunkSize = 40;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1];
            srcStream.Write(encodedLength);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("chunk length", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataError()
        {
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            int maxChunkSize = 40;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            encodedLength[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = 77;
            srcStream.Write(encodedLength);
            srcStream.Write(new byte[76]);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInvalidChunkError()
        {
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            byte maxChunkSize = 100;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            encodedLength[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = maxChunkSize;
            srcStream.Write(encodedLength);
            srcStream.Write(new byte[maxChunkSize]);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("invalid chunk", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInvalidChunkLengthError()
        {
            var srcStream = new MemoryStream();
            var reader = new StreamCustomReaderWriter(srcStream);
            byte maxChunkSize = 100;
            var encodedLength = new byte[] { 0xf0, 1, 3 };
            srcStream.Write(encodedLength);
            srcStream.Write(new byte[maxChunkSize]);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);
            });
            Assert.Contains("negative chunk size", decodingError.Message);
        }
    }
}
