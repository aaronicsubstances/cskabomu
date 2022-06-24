using Kabomu.Common;
using Kabomu.Common.Bodies;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Common.Bodies
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
                var encodedLength = new byte[3];
                ByteUtils.SerializeUpToInt64BigEndian(serializedLength,
                    encodedLength, 0, encodedLength.Length);
                inputStream.Write(encodedLength);
                foreach (var item in serialized)
                {
                    inputStream.Write(item.Data, item.Offset, item.Length);
                }
            }

            // end with terminator empty chunk.            
            inputStream.Write(new byte[] { 0, 0, 2 });
            inputStream.Write(new byte[] { LeadChunk.Version01, 0 });

            inputStream.Position = 0; // rewind position for reads.

            var endOfInputSeen = false;
            var body = new ConfigurableQuasiHttpBody
            {
                ContentType = contentType,
                ReadBytesCallback = async (data, offset, length) =>
                {
                    int bytesRead = 0;
                    Exception e = null;
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
                }
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
        public Task TestReadWithTransportError()
        {
            // arrange.
            var dataList = new string[] { "de", "a" };
            int readIndex = 0;
            var wrappedBody = new ConfigurableQuasiHttpBody
            {
                ContentType = "image/gif",
                ReadBytesCallback = async (data, offset, length) =>
                {
                    Exception e = null;
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

            // act and assert.
            return CommonBodyTestRunner.RunCommonBodyTest(2, instance, -1, "image/gif",
                new int[] { 2, 1 }, "END", null);
        }

        [Fact]
        public Task TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkDecodingBody(null, 100);
            });
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkDecodingBody(null, 0);
            });
            var instance = new ChunkDecodingBody(CreateWrappedBody(null, new string[0]), 100);
            return CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
