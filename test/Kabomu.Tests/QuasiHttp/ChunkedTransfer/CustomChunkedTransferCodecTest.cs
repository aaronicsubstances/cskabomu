using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.QuasiHttp.EntityBody;
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
    public class CustomChunkedTransferCodecTest
    {
        [Fact]
        public async Task TestCodecInternalsWithoutChunkLengthEncoding1()
        {
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01
            };
            var inputStream = new MemoryStream();
            var instance = new CustomChunkedTransferCodec();
            instance.UpdateSerializedRepresentation(expectedChunk);
            int computedByteCount = instance.CalculateSizeInBytesOfSerializedRepresentation();
            await instance.WriteOutSerializedRepresentation(inputStream);
            var actualBytes = inputStream.ToArray();

            var expectedBytes = ByteUtils.StringToBytes(
                "\u0001\u00000,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(expectedBytes.Length, computedByteCount);
            
            var actualChunk = CustomChunkedTransferCodec.Deserialize(
                actualBytes, 0, actualBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public void TestCodecInternalsWithoutChunkLengthEncoding2()
        {
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01
            };
            var equivalentBytes = ByteUtils.StringToBytes(
                "\u0001\u0000true,\"\",0,0,false,\"\",\"\",\"\",2,\"\"\n");
            var actualChunk = CustomChunkedTransferCodec.Deserialize(
                equivalentBytes, 0, equivalentBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestCodecInternalsWithoutChunkLengthEncoding3()
        {
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
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
            var instance = new CustomChunkedTransferCodec();
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

            var actualChunk = CustomChunkedTransferCodec.Deserialize(
                actualBytes, 0, actualBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public void TestCodecInternalsWithoutChunkLengthEncoding4()
        {
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 2,
                RequestTarget = "/detail",
                HttpStatusMessage = "ok",
                ContentLength = 20,
                StatusCode = 200,
                HttpVersion = "1.0",
                Method = "POST",
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedChunk.Headers.Add("accept", new List<string> { "text/plain", "", "text/xml" });
            expectedChunk.Headers.Add("b", new List<string> { "myinside\u00c6.team" });

            var equivalentBytes = ByteUtils.StringToBytes(
                "\u0001\u00021,/detail,200,20,1,POST,1,1.0,1,ok\n" +
                "accept,text/plain\n" +
                "accept,\"\"\n" +
                "accept\n" +
                "a\n" +
                "b,myinside\u00c6.team\n" +
                "accept,text/xml\n");

            var actualChunk = CustomChunkedTransferCodec.Deserialize(
                equivalentBytes, 0, equivalentBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestCodecInternalsWithoutChunkLengthEncoding5()
        {
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 2,
                RequestTarget = "/detail",
                HttpStatusMessage = "ok",
                ContentLength = 20,
                StatusCode = 200,
                HttpVersion = "1.0",
                Method = "POST",
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedChunk.Headers.Add("accept", new List<string> { null, "text/plain", "text/xml" });
            expectedChunk.Headers.Add("a", null);
            expectedChunk.Headers.Add("b", new List<string> { "myinside\u00c6.team" });

            var inputStream = new MemoryStream();
            var instance = new CustomChunkedTransferCodec();
            instance.UpdateSerializedRepresentation(expectedChunk);
            int computedByteCount = instance.CalculateSizeInBytesOfSerializedRepresentation();
            await instance.WriteOutSerializedRepresentation(inputStream);
            var actualBytes = inputStream.ToArray();

            var expectedBytes = ByteUtils.StringToBytes(
                "\u0001\u00021,/detail,200,20,1,POST,1,1.0,1,ok\n" +
                "accept,\"\",text/plain,text/xml\n" +
                "b,myinside\u00c6.team\n");
            Assert.Equal(expectedBytes, actualBytes);
            Assert.Equal(expectedBytes.Length, computedByteCount);
        }

        [Fact]
        public void TestCodecInternalsWithoutChunkLengthEncoding6()
        {
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 0,
                RequestTarget = "http://www.yoursite.com/category/keyword1,keyword2",
                HttpStatusMessage = "ok",
                ContentLength = 140_737_488_355_327,
                StatusCode = 2_147_483_647,
                Method = "PUT",
                Headers = new Dictionary<string, IList<string>>()
            };
            expectedChunk.Headers.Add("content-type", new List<string> { "application/json" });
            expectedChunk.Headers.Add("allow", new List<string> { "GET,POST" });

            var srcBytes = ByteUtils.StringToBytes(
                "\u0001\u00001,\"http://www.yoursite.com/category/keyword1,keyword2\"," +
                "2147483647,140737488355327,1,PUT,0,\"\",1,ok\n" +
                "content-type,application/json\n" +
                "allow,\"GET,POST\"\n");

            var actualChunk = CustomChunkedTransferCodec.Deserialize(
                srcBytes, 0, srcBytes.Length);
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors1()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomChunkedTransferCodec.Deserialize(null, 0, 6);
            });
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors2()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CustomChunkedTransferCodec.Deserialize(new byte[6], 6, 1);
            });
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors3()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CustomChunkedTransferCodec.Deserialize(new byte[7], 0, 7);
            });
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors4()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                CustomChunkedTransferCodec.Deserialize(new byte[] { 1, 2, 3, 4, 5, 6, 7, 0, 0, 0, 9 }, 0, 11);
            });
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors5()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var data = new byte[] { 0, 0, (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',',(byte)'1', (byte)',',(byte)'1', (byte)',',(byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)',', (byte)'1', (byte)',',
                    (byte)'1', (byte)',', (byte)'1', (byte)'\n' };
                CustomChunkedTransferCodec.Deserialize(data, 0, data.Length);
            });
            Assert.Contains("version", ex.Message);
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors6()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var data = ByteUtils.StringToBytes(
                    "\u0001\u00001,\"http://www.yoursite.com/category/keyword1,keyword2\"," +
                    "2147483647,140737488355328,1,PUT,0,\"\",1,ok\n" +
                    "content-type,application/json\n" +
                    "allow,\"GET,POST\"\n");
                CustomChunkedTransferCodec.Deserialize(data, 0, data.Length);
            });
            Assert.Contains("invalid content length", ex.Message);
        }

        [Fact]
        public void TestDeserializationInternalsWithoutChunkLengthDecodingForErrors7()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                var data = ByteUtils.StringToBytes(
                    "\u0001\u00001,\"http://www.yoursite.com/category/keyword1,keyword2\"," +
                    "2147483648,140737488355327,1,PUT,0,\"\",1,ok\n" +
                    "content-type,application/json\n" +
                    "allow,\"GET,POST\"\n");
                CustomChunkedTransferCodec.Deserialize(data, 0, data.Length);
            });
            Assert.Contains("invalid status code", ex.Message);
        }

        [Theory]
        [MemberData(nameof(CreateTestEncodeSubsequentChunkV1HeaderData))]
        public async Task TestEncodeSubsequentChunkV1Header(int chunkDataLength,
            byte[] expected)
        {
            var destStream = new MemoryStream();
            await new CustomChunkedTransferCodec().EncodeSubsequentChunkV1Header(
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
            int expected)
        {
            var actual = await new CustomChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                srcData, null);
            Assert.Equal(expected, actual);

            // should work indirectly with reader
            actual = await new CustomChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                null, new MemoryStream(srcData));
            Assert.Equal(expected, actual);
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkV1HeaderData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[] { 0, 0, 2, 1, 0 };
            int expected = 0;
            testData.Add(new object[] { srcData, expected });

            srcData = new byte[] { 0, 0xea, 0x62, 1, 0 };
            expected = 60_000;
            testData.Add(new object[] { srcData, expected });

            srcData = new byte[] { 0, 0, 3, 1, 0 };
            expected = 1;
            testData.Add(new object[] { srcData, expected });

            srcData = new byte[] { 0, 2, 0x2d, 1, 0 };
            expected = 555;
            testData.Add(new object[] { srcData, expected });

            srcData = new byte[] { 7, 0xce, 0xb3, 1, 0 };
            expected = 511_665;
            testData.Add(new object[] { srcData, expected });

            return testData;
        }

        [Fact]
        public async Task TestDecodeSubsequentChunkV1HeaderForArgError()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                new CustomChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                    null, null));
        }

        [Theory]
        [MemberData(nameof(CreateTestDecodeSubsequentChunkV1HeaderForErrorsData))]
        public async Task TestDecodeSubsequentChunkV1HeaderForErrors(byte[] srcData)
        {
            await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                new CustomChunkedTransferCodec().DecodeSubsequentChunkV1Header(
                    srcData, null));
        }

        public static List<object[]> CreateTestDecodeSubsequentChunkV1HeaderForErrorsData()
        {
            var testData = new List<object[]>();

            var srcData = new byte[] { 0xf7, 2, 9, 1, 0 }; // negative
            testData.Add(new object[] { srcData });

            srcData = new byte[] { 0, 2, 9, 0, 0 }; // version not set
            testData.Add(new object[] { srcData });

            return testData;
        }

        [Fact]
        public async Task TestWriteLeadChunk1()
        {
            // arrange.
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01
            };
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "0,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            var expectedStream = new MemoryStream();
            expectedStream.Write(new byte[] { 0, 0, 26, 1, 0 });
            expectedStream.Write(serializedLeadChunkSuffix);
            var expectedStreamContents = expectedStream.ToArray();

            var destStream = new MemoryStream();

            // act.
            await new CustomChunkedTransferCodec().WriteLeadChunk(
                destStream, leadChunk);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunk2()
        {
            // arrange.
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 5
            };
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "0,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            var expectedStream = new MemoryStream();
            expectedStream.Write(new byte[] { 0, 0, 26, 1, 5 });
            expectedStream.Write(serializedLeadChunkSuffix);
            var expectedStreamContents = expectedStream.ToArray();

            var destStream = new MemoryStream();

            // act.
            await new CustomChunkedTransferCodec().WriteLeadChunk(
                destStream, leadChunk, -1);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunk3()
        {
            // arrange.
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
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
            await new CustomChunkedTransferCodec().WriteLeadChunk(
                writer, leadChunk, 100);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunk4()
        {
            // arrange
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 0
            };
            leadChunk.Headers = new Dictionary<string, IList<string>>();
            leadChunk.Headers.Add("h".PadLeft(70_000), new List<string> { "1" });

            var expected = new MemoryStream();
            expected.Write(new byte[] { 1, 0x11, 0x8d, 1, 0 });
            expected.Write(ByteUtils.StringToBytes("0,\"\",0,0,0,\"\",0,\"\",0,\"\"\n"));
            expected.Write(ByteUtils.StringToBytes(("h".PadLeft(70_000) + ",1\n")));

            var destStream = new MemoryStream();

            // act.
            await new CustomChunkedTransferCodec().WriteLeadChunk(
                destStream, leadChunk,
                CustomChunkedTransferCodec.HardMaxChunkSizeLimit);

            // assert
            Assert.Equal(expected.ToArray(), destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunkForError1()
        {
            // arrange.
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
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
            var destStream = new MemoryStream();
            var writer = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (dta, offset, leng) =>
                    destStream.WriteAsync(dta, offset, leng)
            };

            // act.
            var actualEx = await Assert.ThrowsAsync<ChunkEncodingException>(async () =>
            {
                await new CustomChunkedTransferCodec().WriteLeadChunk(
                    writer, leadChunk, 10);
            });
            Assert.Contains("headers", actualEx.Message);
            Assert.Contains("exceed", actualEx.Message);
            Assert.Contains("chunk size", actualEx.Message);
        }

        [Fact]
        public async Task TestWriteLeadChunkForError2()
        {
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 1,
                RequestTarget = "1",
                StatusCode = 1,
                ContentLength = 1,
                Method = "1",
                HttpVersion = "1",
                HttpStatusMessage = "1"
            };
            leadChunk.Headers = new Dictionary<string, IList<string>>();
            for (int i = 0; i < 40_000; i++)
            {
                var key = $"{i}".PadLeft(5, '0');
                leadChunk.Headers.Add(key, new List<string> { "1" });
            }

            // act.
            var actualEx = await Assert.ThrowsAsync<ChunkEncodingException>(async () =>
            {
                await new CustomChunkedTransferCodec().WriteLeadChunk(
                    new MemoryStream(), leadChunk);
            });
            Assert.Contains("headers", actualEx.Message);
            Assert.Contains("exceed", actualEx.Message);
            Assert.Contains("chunk size", actualEx.Message);
        }

        [Fact]
        public async Task TestWriteLeadChunkForError3()
        {
            var leadChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 1,
                RequestTarget = "1",
                StatusCode = 1,
                ContentLength = 1,
                Method = "1",
                HttpVersion = "1",
                HttpStatusMessage = "1"
            };
            leadChunk.Headers = new Dictionary<string, IList<string>>();
            for (int i = 0; i < 40_000; i++)
            {
                var key = $"{i}".PadLeft(5, '0');
                leadChunk.Headers.Add(key, new List<string> { "1" });
            }

            // act.
            var actualEx = await Assert.ThrowsAsync<ChunkEncodingException>(async () =>
            {
                await new CustomChunkedTransferCodec().WriteLeadChunk(
                    new MemoryStream(), leadChunk,
                    CustomChunkedTransferCodec.HardMaxChunkSizeLimit + 1);
            });
            Assert.Contains("headers", actualEx.Message);
            Assert.Contains("exceed", actualEx.Message);
            Assert.Contains("chunk size", actualEx.Message);
        }

        [Fact]
        public async Task TestReadLeadChunk1()
        {
            // arrange.
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "0,\"\",0,0,0,\"\",0,\"\",0,\"\"\n");
            var srcStream = new MemoryStream();
            srcStream.Write(new byte[] { 0, 0, 26, 1, 0 });
            srcStream.Write(serializedLeadChunkSuffix);
            srcStream.Position = 0; // reset for reading.
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01
            };

            // act
            var actualChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                srcStream);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunk2()
        {
            // arrange.
            var serializedLeadChunkSuffix = ByteUtils.StringToBytes(
                "0,\"\",100,1,0,\"\",0,\"\",0,\"\"\n");
            var srcStream = new MemoryStream();
            srcStream.Write(new byte[] { 0, 0, 28, 1, 0 });
            srcStream.Write(serializedLeadChunkSuffix);
            srcStream.Position = 0; // reset for reading.
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                StatusCode = 100,
                ContentLength = 1
            };

            // act
            var actualChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(
                srcStream, -1);

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
            var actualChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(reader, maxChunkSize);

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
                Version = CustomChunkedTransferCodec.Version01,
                RequestTarget = "/abcdefghijklmop"
            };

            // act
            var actualChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(reader, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForDefaultMaxChunkLimitExceeded()
        {
            // arrange
            var srcStream = new MemoryStream();
            // length = 320_022, which exceeds 64kb
            srcStream.Write(new byte[] { 0x04, 0xe2, 0x16, 1, 1 });
            srcStream.Write(ByteUtils.StringToBytes(
                "1,1,1,1,1,1,1,1,1,1\n"));
            for (int i = 0; i < 40_000; i++)
            {
                var key = $"{i}".PadLeft(5, '0');
                srcStream.Write(ByteUtils.StringToBytes($"{key},1\n"));
            }
            srcStream.Position = 0; // reset for reading.
            int maxChunkSize = 400_000;
            var expectedChunk = new LeadChunk
            {
                Version = CustomChunkedTransferCodec.Version01,
                Flags = 1,
                RequestTarget = "1",
                StatusCode = 1,
                ContentLength = 1,
                Method = "1",
                HttpVersion = "1",
                HttpStatusMessage = "1"
            };
            expectedChunk.Headers = new Dictionary<string, IList<string>>();
            for (int i = 0; i < 40_000; i++)
            {
                var key = $"{i}".PadLeft(5, '0');
                expectedChunk.Headers.Add(key, new List<string> { "1" });
            }

            // act
            var actualChunk = await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);

            // assert
            ComparisonUtils.CompareLeadChunks(expectedChunk, actualChunk);
        }

        [Fact]
        public async Task TestReadLeadChunkForMaxChunkExceededError1()
        {
            var srcStream = new MemoryStream(
                new byte[] { 0xf, 0x42, 0x40 }); // length of 1 million
            int maxChunkSize = 40;

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("quasi http headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("exceed", decodingError.InnerException.Message);
            Assert.Contains("chunk size", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForMaxChunkExceededError2()
        {
            var srcStream = new MemoryStream(
                new byte[] { 0xf, 0x42, 0x40 }); // length of 1 million
            int maxChunkSize = 400_000;

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("quasi http headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("exceed", decodingError.InnerException.Message);
            Assert.Contains("chunk size", decodingError.InnerException.Message);
        }

        [Fact]
        public async Task TestReadLeadChunkForInsuffcientDataForLengthError()
        {
            var srcStream = new MemoryStream(
                new byte[2]); // insufficient because even prefix length is 3 bytes
            int maxChunkSize = 40;

            var decodingError = await Assert.ThrowsAsync<ChunkDecodingException>(async () =>
            {
                await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("quasi http headers", decodingError.Message);
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
                await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("quasi http headers", decodingError.Message);
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
                await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("quasi http headers", decodingError.Message);
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
                await new CustomChunkedTransferCodec().ReadLeadChunk(srcStream, maxChunkSize);
            });
            Assert.Contains("quasi http headers", decodingError.Message);
            Assert.NotNull(decodingError.InnerException);
            Assert.Contains("negative chunk size", decodingError.InnerException.Message);
        }

        [Fact]
        public void TestUpdateRequest()
        {
            var request = new DefaultQuasiHttpRequest();
            var headers = new Dictionary<string, IList<string>>
            {
                { "a", new string[]{ "apple" } }
            };
            var leadChunk = new LeadChunk
            {
                Version = 0,
                Method = "GET",
                RequestTarget = "/",
                Headers = headers,
                HttpVersion = "1.0"
            };
            CustomChunkedTransferCodec.UpdateRequest(request, leadChunk);
            Assert.Equal("GET", request.Method);
            Assert.Equal("/", request.Target);
            Assert.Same(headers, request.Headers);
            Assert.Equal("1.0", request.HttpVersion);
        }

        [Fact]
        public void TestUpdateResponse()
        {
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = 0
            };
            var headers = new Dictionary<string, IList<string>>
            {
                { "b", new List<string>{ "ball", "BALL"} }
            };
            var leadChunk = new LeadChunk
            {
                Version = 0,
                StatusCode = 202,
                HttpStatusMessage = "No content",
                Headers = headers,
                HttpVersion = "1.1"
            };
            CustomChunkedTransferCodec.UpdateResponse(response, leadChunk);
            Assert.Equal(202, response.StatusCode);
            Assert.Equal("No content", response.HttpStatusMessage);
            Assert.Same(headers, response.Headers);
            Assert.Equal("1.1", response.HttpVersion);
        }

        [Fact]
        public void TestCreateFromRequest1()
        {
            // arrange
            var request = new DefaultQuasiHttpRequest();
            var expected = new LeadChunk
            {
                Version = 1
            };

            // act
            var actual = CustomChunkedTransferCodec.CreateFromRequest(request);

            // assert
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public void TestCreateFromRequest2()
        {
            // arrange
            var headers = new Dictionary<string, IList<string>>
            {
                { "c", new string[]{ "can" } }
            };
            var body = new StringBody("");
            body.ContentLength = -13;
            var request = new DefaultQuasiHttpRequest
            {
                Target = "/index.html",
                Method = "POST",
                Headers = headers,
                HttpVersion = "2",
                Body = body
            };
            var expected = new LeadChunk
            {
                Version = 1,
                RequestTarget = "/index.html",
                Method = "POST",
                Headers = headers,
                HttpVersion = "2",
                ContentLength = -13
            };

            // act
            var actual = CustomChunkedTransferCodec.CreateFromRequest(request);

            // assert
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public void TestCreateFromRequest3()
        {
            // arrange
            var headers = new Dictionary<string, IList<string>>();
            var body = new StringBody("");
            body.ContentLength = 40;
            var request = new DefaultQuasiHttpRequest
            {
                Target = "",
                Method = "",
                Headers = headers,
                HttpVersion = "",
                Body = body
            };
            var expected = new LeadChunk
            {
                Version = 1,
                RequestTarget = "",
                Method = "",
                Headers = headers,
                HttpVersion = "",
                ContentLength = 40
            };

            // act
            var actual = CustomChunkedTransferCodec.CreateFromRequest(request);

            // assert
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public void TestCreateFromResponse1()
        {
            // arrange
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = -1
            };
            var expected = new LeadChunk
            {
                Version = 1,
                StatusCode = -1
            };

            // act
            var actual = CustomChunkedTransferCodec.CreateFromResponse(response);

            // assert
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public void TestCreateFromResponse2()
        {
            // arrange
            var headers = new Dictionary<string, IList<string>>
            {
                { "b", new List<string>{ "ball", "BALL" } }
            };
            var body = new StringBody("pineapple");
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = 200,
                HttpStatusMessage = "Ok",
                Headers = headers,
                HttpVersion = "1.1",
                Body = body
            };
            var expected = new LeadChunk
            {
                Version = 1,
                StatusCode = 200,
                HttpStatusMessage = "Ok",
                Headers = headers,
                HttpVersion = "1.1",
                ContentLength = 9
            };

            // act
            var actual = CustomChunkedTransferCodec.CreateFromResponse(response);

            // assert
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }

        [Fact]
        public void TestCreateFromResponse3()
        {
            // arrange
            var headers = new Dictionary<string, IList<string>>();
            var body = new StringBody("v");
            body.ContentLength = 0;
            var response = new DefaultQuasiHttpResponse
            {
                StatusCode = 4,
                HttpStatusMessage = "",
                Headers = headers,
                HttpVersion = "",
                Body = body
            };
            var expected = new LeadChunk
            {
                Version = 1,
                StatusCode = 4,
                HttpStatusMessage = "",
                Headers = headers,
                HttpVersion = "",
                ContentLength = 0
            };

            // act
            var actual = CustomChunkedTransferCodec.CreateFromResponse(response);

            // assert
            ComparisonUtils.CompareLeadChunks(expected, actual);
        }
    }
}
