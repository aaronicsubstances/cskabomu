using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.Tests.Shared.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.ChunkedTransfer
{
    public class ChunkedTransferCodecTest
    {
        [Fact]
        public async Task TestCodecInternalsWithoutChunkLengthEncoding1()
        {
            var expectedChunk = new LeadChunk
            {
                Version = ChunkedTransferCodec.Version01
            };
            var inputStream = new MemoryStream();
            var instance = new ChunkedTransferCodec();
            instance.UpdateSerializedRepresentation(expectedChunk);
            int computedByteCount = instance.CalculateSizeInBytesOfSerializedRepresentation();
            await instance.WriteOutSerializedRepresentation(inputStream);
            var actualBytes = inputStream.ToArray();

            var expectedBytes = ByteUtils.StringToBytes(
                "\u0001\u00000,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(expectedBytes.Length, computedByteCount);
            
            var actualChunk = ChunkedTransferCodec.Deserialize(
                actualBytes, 0, actualBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestCodecInternalsWithoutChunkLengthEncoding2()
        {
            var expectedChunk = new LeadChunk
            {
                Version = ChunkedTransferCodec.Version01,
                Flags = 2,
                RequestTarget = "/detail",
                HttpStatusMessage = "ok",
                ContentLength = 20,
                StatusCode = 200,
                HttpVersion = "1.0",
                Method = "POST",
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedChunk.Headers.Add("accept", new List<string> { "text/plain", "text/xml" });
            expectedChunk.Headers.Add("a", new List<string>());
            expectedChunk.Headers.Add("b", new List<string> { "myinside\u00c6.team" });

            var inputStream = new MemoryStream();
            var instance = new ChunkedTransferCodec();
            instance.UpdateSerializedRepresentation(expectedChunk);
            int computedByteCount = instance.CalculateSizeInBytesOfSerializedRepresentation();
            await instance.WriteOutSerializedRepresentation(inputStream);
            var actualBytes = inputStream.ToArray();

            var expectedBytes = ByteUtils.StringToBytes(
                "\u0001\u00021,/detail,200,20,1,POST,1,1.0,1,ok\n" +
                "accept,text/plain,text/xml\n" +
                "b,myinside\u00c6.team\n");
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(expectedBytes.Length, computedByteCount);

            var actualChunk = ChunkedTransferCodec.Deserialize(
                actualBytes, 0, actualBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                ChunkedTransferCodec.Deserialize(null, 0, 6);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ChunkedTransferCodec.Deserialize(new byte[6], 6, 1);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ChunkedTransferCodec.Deserialize(new byte[7], 0, 7);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ChunkedTransferCodec.Deserialize(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 9 }, 0, 11);
            });
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var data = new byte[] { 0, 0, (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',',(byte)'1', (byte)',',(byte)'1', (byte)',',(byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)'\n' };
                ChunkedTransferCodec.Deserialize(data, 0, data.Length);
            });
            Assert.Contains("version", ex.Message);
        }

        [Theory]
        [MemberData(nameof(CreateTestEncodeSubsequentChunkV1HeaderData))]
        public async Task TestEncodeSubsequentChunkV1Header(int chunkDataLength,
            byte[] expected)
        {
            var destStream = new MemoryStream();
            await new ChunkedTransferCodec().EncodeSubsequentChunkV1Header(
                chunkDataLength, destStream);
            var actual = destStream.ToArray();
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
            var actual = await new ChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                srcData, null, maxChunkSize);
            Assert.Equal(expected, actual);

            // should work indirectly with reader
            actual = await new ChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                null, new MemoryStream(srcData), maxChunkSize);
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

        [Fact]
        public async Task TestDecodeSubsequentChunkV1HeaderForArgError()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                new ChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                    null, null, 0));
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeSubsequentChunkV1HeaderForErrorsData))]
        public async Task TestDecodeSubsequentChunkV1HeaderForErrors(byte[] srcData,
            int maxChunkSize)
        {
            await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                new ChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                    srcData, null, maxChunkSize));
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkV1HeaderForErrorsData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[] { 7, 0xce, 0xb3, 1, 0 }; // 511,665
            var maxChunkSize = 65_536;
            testData.Add(new object[] { srcData, maxChunkSize });

            srcData = new byte[] { 0xf7, 2, 9, 1, 0 }; // negative
            maxChunkSize = 30_000;
            testData.Add(new object[] { srcData, maxChunkSize });

            srcData = new byte[] { 0, 2, 9, 0, 0 }; // version not set
            maxChunkSize = 15_437;
            testData.Add(new object[] { srcData, maxChunkSize });

            return testData;
        }

        [Fact]
        public async Task TestWriteLeadChunk1()
        {
            // arrange.
            var leadChunk = new LeadChunk
            {
                Version = ChunkedTransferCodec.Version01
            };
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "0,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            var expectedStream = new MemoryStream();
            expectedStream.Write(new byte[] { 0, 0, 26, 1, 0 });
            expectedStream.Write(serializedLeadChunkSuffix);
            var expectedStreamContents = expectedStream.ToArray();

            var destStream = new MemoryStream();

            // act.
            await new ChunkedTransferCodec().WriteLeadChunk(destStream, leadChunk, -1);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunk2()
        {
            // arrange.
            var leadChunk = new LeadChunk
            {
                Version = ChunkedTransferCodec.Version01,
                Flags = 3,
                RequestTarget = "/foo/bar",
                StatusCode = 201,
                ContentLength = -4000,
                Method = "GET",
                HttpVersion = "1.1",
                HttpStatusMessage = "Accepted for processing",
                Headers = new Dictionary<string, IList<string>>
                {
                    { "zero", new List<string>{ } },
                    { "one", new List<string>{ "1"} },
                    { "two", new List<string>{ "2", "2"} }
                }
            };
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "1,/foo/bar,201,-4000,1,GET,1,1.1,1,Accepted for processing\n" +
                "one,1\n" +
                "two,2,2\n"
            );
            var expectedStream = new MemoryStream();
            expectedStream.Write(new byte[] { 0, 0, 75, 1, 3 });
            expectedStream.Write(serializedLeadChunkSuffix);
            var expectedStreamContents = expectedStream.ToArray();

            var destStream = new MemoryStream();
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (dta, offset, leng) =>
                    destStream.WriteAsync(dta, offset, leng)
            };

            // act.
            await new ChunkedTransferCodec().WriteLeadChunk(writer, leadChunk, 1000);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestReadLeadChunk()
        {
            // arrange.
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "0,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            var srcStream = new MemoryStream();
            srcStream.Write(new byte[] { 0, 0, 26, 1, 0 });
            srcStream.Write(serializedLeadChunkSuffix);
            srcStream.Position = 0; // reset for reading.
            int maxChunkSize = 0;
            var expectedChunk = new LeadChunk
            {
                Version = ChunkedTransferCodec.Version01
            };

            // act
            var actualChunk = await new ChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);

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
            var actualChunk = await new ChunkedTransferCodec().ReadLeadChunk(reader, maxChunkSize);

            // assert
            Assert.Null(actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForLaxityInChunkSizeCheck()
        {
            // arrange.
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "1,/abcdefghijklmop,0,0,0,\"\",0,\"\",0,\"\"\n");
            var srcStream = new MemoryStream();
            srcStream.Write(new byte[] { 0, 0, 40, 1, 0 });
            srcStream.Write(serializedLeadChunkSuffix);
            srcStream.Position = 0; // reset for reading.
            var reader = new LambdaBasedCustomReaderWriter
            {
                ReadFunc = (data, offset, length) =>
                    srcStream.ReadAsync(data, offset, length)
            };
            int maxChunkSize = 10; // definitely less than actual serialized value
                                   // but ok once it is less than 64K
            var expectedChunk = new LeadChunk
            {
                Version = ChunkedTransferCodec.Version01,
                RequestTarget = "/abcdefghijklmop"
            };

            // act
            var actualChunk = await new ChunkedTransferCodec().ReadLeadChunk(reader, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForMaxChunkExceededError()
        {
            var srcStream = new MemoryStream(
                new byte[] { 0xf, 0x42, 0x40 }); // length of 1 million
            int maxChunkSize = 40;

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new ChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("exceed", decodingError.InnerException.Message);
            Assert.Contains("chunk size", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataForLengthError()
        {
            var srcStream = new MemoryStream(new byte[ChunkedTransferCodec.LengthOfEncodedChunkLength - 1]);
            int maxChunkSize = 40;

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new ChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
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
            srcStream.Write(new byte[] { 0, 0, 77 });
            srcStream.Write(new byte[76]);
            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new ChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("unexpected end of read", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForLeadChunkDeserializationError()
        {
            var srcStream = new MemoryStream();
            int maxChunkSize = 100;
            srcStream.Write(new byte[] { 0, 0, 100 });
            srcStream.Write(new byte[100]); // version not set
            srcStream.Position = 0; // reset for reading.

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new ChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.Contains("invalid chunk", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("version", decodingError.InnerException.Message);
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
                await new ChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("negative chunk size", decodingError.InnerException.Message);
        }
    }
}
