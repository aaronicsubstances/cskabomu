using Kabomu.Common;
using Kabomu.Internals;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public class ByteOrientedTransferBodyTest
    {
        private static IQuasiHttpTransport CreateTransport(object connection, string[] dataChunks)
        {
            var inputStream = new MemoryStream();
            foreach (var dataChunk in dataChunks)
            {
                var dataChunkBytes = Encoding.UTF8.GetBytes(dataChunk);
                var encodedLength = new byte[2];
                ByteUtils.SerializeUpToInt64BigEndian(dataChunkBytes.Length,
                    encodedLength, 0, encodedLength.Length);
                inputStream.Write(encodedLength);
                inputStream.Write(dataChunkBytes);
            }
            inputStream.Write(new byte[2]); // terminate with zero-byte chunk
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
            var instance = new ByteOrientedTransferBody(null, transport, "lo", closeCb);

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
            var instance = new ByteOrientedTransferBody("text/xml", transport, null, closeCb);

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
            var instance = new ByteOrientedTransferBody("text/xml", transport, null, closeCb);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "text/xml",
                new int[] { 1, 1, 1, 1, 1, 1, 1, 1 }, null, "car seat");
            Assert.True(closed);
        }

        [Fact]
        public void TestReadWithTransportError()
        {
            // arrange.
            var dataList = new string[] { "de", "al" };
            int readIndex = 0;
            IQuasiHttpTransport transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (connection, data, offset, length, cb) =>
                {
                    Assert.Equal(1786, connection);
                    int bytesRead = 0;
                    Exception e = null;
                    if (readIndex/2 < dataList.Length)
                    {
                        var nextChunk = dataList[readIndex/2];
                        if (readIndex % 2 == 0)
                        {
                            ByteUtils.SerializeUpToInt64BigEndian(nextChunk.Length,
                                data, offset, 2);
                            bytesRead = 2;
                        }
                        else
                        {
                            var nextChunkBytes = Encoding.UTF8.GetBytes(nextChunk);
                            Array.Copy(nextChunkBytes, 0, data, offset, nextChunkBytes.Length);
                            bytesRead = nextChunkBytes.Length;
                        }
                        readIndex++;
                    }
                    else
                    {
                        e = new Exception("END");
                    }
                    cb.Invoke(e, bytesRead);
                }
            };
            var instance = new ByteOrientedTransferBody("image/gif", transport, 1786, null);

            // act and assert.
            CommonBodyTestRunner.RunCommonBodyTest(instance, "image/gif",
                new int[] { 2, 2 }, "END", null);
        }

        [Fact]
        public void TestForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ByteOrientedTransferBody(null, null, null, () => { });
            });
            var instance = new ByteOrientedTransferBody(null, 
                CreateTransport(null, new string[0]), null, () => { });
            CommonBodyTestRunner.RunCommonBodyTestForArgumentErrors(instance);
        }
    }
}
