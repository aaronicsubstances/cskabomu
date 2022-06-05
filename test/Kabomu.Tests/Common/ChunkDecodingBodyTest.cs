using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Common
{
    public class ChunkDecodingBodyTest
    {
        private static IQuasiHttpTransport CreateTransport(object connection, string[] strings)
        {
            var inputStream = new MemoryStream();
            foreach (var s in strings)
            {
                var bytes = Encoding.UTF8.GetBytes(s);
                var chunk = new SubsequentChunk
                {
                    Data = bytes,
                    DataLength = bytes.Length
                };
                var serialized = chunk.Serialize();
                var serializedLength = serialized.Sum(x => x.Length);
                var encodedLength = new byte[2];
                ByteUtils.SerializeUpToInt64BigEndian(serializedLength,
                    encodedLength, 0, encodedLength.Length);
                inputStream.Write(encodedLength);
                foreach (var item in serialized)
                {
                    inputStream.Write(item.Data, item.Offset, item.Length);
                }
            }

            // end with terminator empty chunk.
            inputStream.Write(new byte[] { 0, 2 });
            inputStream.Write(new byte[2]);

            inputStream.Position = 0; // rewind position for reads.

            var endOfInputSeen = false;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    int bytesRead = 0;
                    Exception e = null;
                    if (endOfInputSeen)
                    {
                        e = new Exception("END");
                    }
                    else
                    {
                        bytesRead = inputStream.Read(data, offset, length);
                        if (bytesRead == 0)
                        {
                            endOfInputSeen = true;
                        }
                    }
                    cb.Invoke(e, bytesRead);
                }
            };
            return transport;
        }

        [Fact]
        public void TestEmptyRead()
        {
            // arrange.
            var dataList = new string[0];
            var transport = CreateTransport("lo", dataList);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ChunkDecodingBody(null, transport, "lo", closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, null,
                new int[0], null, "");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyRead()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var transport = CreateTransport(null, dataList);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ChunkDecodingBody("text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/xml",
                new int[] { 3, 1, 4 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestNonEmptyRead2()
        {
            // arrange.
            var dataList = new string[] { "car", " ", "seat" };
            var transport = CreateTransport(null, dataList);
            var closed = false;
            Action closeCb = () => closed = true;
            var instance = new ChunkDecodingBody("text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/xml",
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestReadWithTransportError()
        {
            // arrange.
            var dataList = new string[] { "de", "a" };
            int readIndex = 0;
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (connection, data, offset, length, cb) =>
                {
                    Assert.Equal(1786, connection);
                    Exception e = null;
                    int bytesRead = 0;
                    switch (readIndex)
                    {
                        case 0:
                            data[offset] = 0;
                            data[offset + 1] = 4;
                            bytesRead = 2;
                            break;
                        case 1:
                            data[offset] = 0;
                            data[offset + 1] = 0;
                            data[offset + 2] = (byte)'d';
                            data[offset + 3] = (byte)'e';
                            bytesRead = 4;
                            break;
                        case 2:
                            data[offset] = 0;
                            data[offset + 1] = 3;
                            bytesRead = 2;
                            break;
                        case 3:
                            data[offset] = 0;
                            data[offset + 1] = 0;
                            data[offset + 2] = (byte)'a';
                            bytesRead = 3;
                            break;
                        default:
                            e = new Exception("END");
                            break;
                    }
                    readIndex++;
                    cb.Invoke(e, bytesRead);
                }
            };
            var instance = new ChunkDecodingBody("image/gif", transport, 1786, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "image/gif",
                new int[] { 2, 1 }, "END", null);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ChunkDecodingBody(null, null, null, () => { });
            });
            var instance = new ChunkDecodingBody(null,
                CreateTransport(null, new string[0]), null, () => { });
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
