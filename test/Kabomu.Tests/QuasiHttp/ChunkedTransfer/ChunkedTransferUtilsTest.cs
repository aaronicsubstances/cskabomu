using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.Tests.Shared;
using Kabomu.Tests.Shared.Common;
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
        public async Task TestEncodeSubsequentChunkHeader(int chunkDataLength,
            byte[] expected)
        {
            var destStream = new MemoryStream();
            var writer = new StreamCustomReaderWriter(destStream);
            await ChunkedTransferUtils.EncodeSubsequentChunkHeader(
                chunkDataLength, writer, new byte[50]);
            var actual = destStream.ToArray();
            ComparisonUtils.CompareData(expected, 0, expected.Length,
                actual, 0, actual.Length);
        }

        public static List<object[]> CreateTestEncodeSubsequentChunkHeaderData()
        {
            var testData = new List<object[]>();

            int chunkDataLength = 0;
            var expected = new byte[] { 0, 0, 2, 1, 0 };
            testData.Add(new object[] { chunkDataLength, expected });

            chunkDataLength = 555;
            expected = new byte[] { 0, 2, 0x2d, 1, 0 };
            testData.Add(new object[] { chunkDataLength, expected });

            chunkDataLength = 511_665;
            expected = new byte[] { 7, 0xce, 0xb3, 1, 0 };
            testData.Add(new object[] { chunkDataLength, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeSubsequentChunkHeaderData))]
        public async Task TestDecodeSubsequentChunkHeader(byte[] srcData,
            int maxChunkSize, int expected)
        {
            var reader = new DemoCustomReaderWriter(srcData);
            var actual = await ChunkedTransferUtils.DecodeSubsequentChunkHeader(
                reader, new byte[50], maxChunkSize);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkHeaderData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[] { 0, 0, 2, 1, 0 };
            int maxChunkSize = 40;
            int expected = 0;
            testData.Add(new object[] { srcData, maxChunkSize, expected });

            srcData = new byte[] { 0, 2, 0x2d, 1, 0 };
            maxChunkSize = 400; // ok because it is below hard limit
            expected = 555;
            testData.Add(new object[] { srcData, maxChunkSize, expected });

            srcData = new byte[] { 7, 0xce, 0xb3, 1, 0 };
            maxChunkSize = 600_000;
            expected = 511_665;
            testData.Add(new object[] { srcData, maxChunkSize, expected });

            return testData;
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeSubsequentChunkHeaderForErrorsData))]
        public async Task TestDecodeSubsequentChunkHeaderForErrors(byte[] srcData,
            int maxChunkSize)
        {
            var reader = new DemoCustomReaderWriter(srcData);
            await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                ChunkedTransferUtils.DecodeSubsequentChunkHeader(
                    reader, new byte[50], maxChunkSize));
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkHeaderForErrorsData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[] { 7, 0xce, 0xb3, 1, 0 }; // 511,665
            var maxChunkSize = 65_536;
            testData.Add(new object[] { srcData, maxChunkSize });

            srcData = new byte[] { 0xf7, 2, 9, 1, 0 }; // negative
            maxChunkSize = 65_536;
            testData.Add(new object[] { srcData, maxChunkSize });

            srcData = new byte[] { 0, 2, 9, 0, 0 }; // version not set
            maxChunkSize = 65_536;
            testData.Add(new object[] { srcData, maxChunkSize });

            return testData;
        }

        [Fact]
        public async Task TestWriteLeadChunk1()
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
            await ChunkedTransferUtils.WriteLeadChunk(writer, 0, leadChunk);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunk2()
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
            int maxChunkSize = 0;
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
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("exceed", decodingError.InnerException.Message);
            Assert.Contains("chunk size", decodingError.InnerException.Message);
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
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
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
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("negative chunk size", decodingError.InnerException.Message);
        }
    }
}
