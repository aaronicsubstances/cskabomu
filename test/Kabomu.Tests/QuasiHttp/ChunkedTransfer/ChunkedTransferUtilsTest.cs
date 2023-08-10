using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
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
        [MemberData(nameof(CreateTestEncodeSubsequentChunkV1HeaderData))]
        public async Task TestEncodeSubsequentChunkV1Header(int chunkDataLength,
            byte[] expected)
        {
            var destStream = new MemoryStream();
            await ChunkedTransferUtils.EncodeSubsequentChunkV1Header(
                chunkDataLength, destStream, new byte[5]);
            var actual = destStream.ToArray();
            ComparisonUtils.CompareData(expected, 0, expected.Length,
                actual, 0, actual.Length);

            // should work without temp buffer
            destStream.Position = 0;
            await ChunkedTransferUtils.EncodeSubsequentChunkV1Header(
                chunkDataLength, destStream);
            actual = destStream.ToArray();
            ComparisonUtils.CompareData(expected, 0, expected.Length,
                actual, 0, actual.Length);

            // should also work with unusable buffer
            destStream.Position = 0;
            await ChunkedTransferUtils.EncodeSubsequentChunkV1Header(
                chunkDataLength, destStream, new byte[4]);
            actual = destStream.ToArray();
            ComparisonUtils.CompareData(expected, 0, expected.Length,
                actual, 0, actual.Length);
        }

        public static List<object[]> CreateTestEncodeSubsequentChunkV1HeaderData()
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
        [MemberData(nameof(CreateTestDecodeSubsequentChunkV1HeaderData))]
        public async Task TestDecodeSubsequentChunkV1Header(byte[] srcData,
            int maxChunkSize, int expected)
        {
            var reader = new MemoryStream(srcData);
            var actual = await ChunkedTransferUtils.DecodeSubsequentChunkV1Header(
                reader, new byte[50], maxChunkSize);
            Assert.Equal(expected, actual);

            // should work without temp buffer
            reader.Position = 0;
            actual = await ChunkedTransferUtils.DecodeSubsequentChunkV1Header(
                reader, null, maxChunkSize);
            Assert.Equal(expected, actual);

            // should also work with unusable buffer
            reader.Position = 0;
            actual = await ChunkedTransferUtils.DecodeSubsequentChunkV1Header(
                reader, new byte[4], maxChunkSize);
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkV1HeaderData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[] { 0, 0, 2, 1, 0 };
            int maxChunkSize = 40;
            int expected = 0;
            testData.Add(new object[] { srcData, maxChunkSize, expected });

            srcData = new byte[] { 0, 0, 2, 1, 0 };
            maxChunkSize = 0;
            expected = 0;
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
        [MemberData(nameof(CreateTestDecodeSubsequentChunkV1HeaderForErrorsData))]
        public async Task TestDecodeSubsequentChunkV1HeaderForErrors(byte[] srcData,
            int maxChunkSize)
        {
            var reader = new MemoryStream(srcData);
            await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                ChunkedTransferUtils.DecodeSubsequentChunkV1Header(
                    reader, new byte[50], maxChunkSize));
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkV1HeaderForErrorsData()
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
            var leadChunk = new LeadChunk();
            var expectedStream = new MemoryStream();
            expectedStream.Write(new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength]);
            var serializedLength = await LeadChunkTest.Serialize(leadChunk, expectedStream);
            var expectedStreamContents = expectedStream.ToArray();
            expectedStreamContents[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = (byte)serializedLength;

            var destStream = new MemoryStream();

            // act.
            await ChunkedTransferUtils.WriteLeadChunk(destStream, leadChunk, -1);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunk2()
        {
            // arrange.
            var leadChunk = new LeadChunk();
            var expectedStream = new MemoryStream();
            expectedStream.Write(new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength]);
            var serializedLength = await LeadChunkTest.Serialize(leadChunk, expectedStream);
            var expectedStreamContents = expectedStream.ToArray();
            expectedStreamContents[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = (byte)serializedLength;

            var destStream = new MemoryStream();
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (dta, offset, leng) =>
                    destStream.WriteAsync(dta, offset, leng)
            };

            // act.
            await ChunkedTransferUtils.WriteLeadChunk(writer, leadChunk, 1000);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestReadLeadChunk()
        {
            // arrange.
            var srcStream = new MemoryStream();
            int maxChunkSize = 0;
            var expectedChunk = new LeadChunk
            {
                Version = LeadChunk.Version01
            };
            srcStream.Write(new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength]);
            var serializedLength = await LeadChunkTest.Serialize(expectedChunk, srcStream);
            srcStream.Position = ChunkedTransferUtils.LengthOfEncodedChunkLength - 1;
            srcStream.WriteByte((byte)serializedLength);
            srcStream.Position = 0; // reset for reading.

            // act
            var actualChunk = await ChunkedTransferUtils.ReadLeadChunk(srcStream, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadNullLeadChunk()
        {
            // arrange.
            var srcStream = new MemoryStream();
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    srcStream.ReadAsync(data, offset, length)
            };
            int maxChunkSize = 0;

            // act
            var actualChunk = await ChunkedTransferUtils.ReadLeadChunk(reader, maxChunkSize);

            // assert
            Assert.Null(actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForLaxityInChunkSizeCheck()
        {
            // arrange.
            var srcStream = new MemoryStream();
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    srcStream.ReadAsync(data, offset, length)
            };
            int maxChunkSize = 10; // definitely less than actual serialized value but ok once it is less than 64K
            var expectedChunk = new LeadChunk
            {
                Version = LeadChunk.Version01,
                RequestTarget = "/abcdefghijklmop"
            };
            srcStream.Write(new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength]);
            var serializedLength = await LeadChunkTest.Serialize(expectedChunk, srcStream);
            srcStream.Position = ChunkedTransferUtils.LengthOfEncodedChunkLength - 1;
            srcStream.WriteByte((byte)serializedLength);
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
            int maxChunkSize = 40;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt32BigEndian(1_000_000, encodedLength, 0, encodedLength.Length);
            srcStream.Write(encodedLength);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(srcStream, maxChunkSize);
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
            int maxChunkSize = 40;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1];
            srcStream.Write(encodedLength);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataError()
        {
            var srcStream = new MemoryStream();
            int maxChunkSize = 40;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            encodedLength[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = 77;
            srcStream.Write(encodedLength);
            srcStream.Write(new byte[76]);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInvalidChunkError()
        {
            var srcStream = new MemoryStream();
            byte maxChunkSize = 100;
            var encodedLength = new byte[ChunkedTransferUtils.LengthOfEncodedChunkLength];
            encodedLength[ChunkedTransferUtils.LengthOfEncodedChunkLength - 1] = maxChunkSize;
            srcStream.Write(encodedLength);
            srcStream.Write(new byte[maxChunkSize]);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("invalid chunk", decodingError.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInvalidChunkLengthError()
        {
            var srcStream = new MemoryStream();
            byte maxChunkSize = 100;
            var encodedLength = new byte[] { 0xf0, 1, 3 };
            srcStream.Write(encodedLength);
            srcStream.Write(new byte[maxChunkSize]);

            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await ChunkedTransferUtils.ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("negative chunk size", decodingError.InnerException.Message);
        }
    }
}
