using Kabomu.Common;
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
    public class ChunkEncodingCustomWriterTest
    {
        [Fact]
        public void TestForConstructionSuccess1()
        {
            var writer = new MemoryStream();
            _ = new ChunkEncodingCustomWriter(writer, 1_000_000);
        }

        [Fact]
        public void TestForConstructionSuccess2()
        {
            var writer = new MemoryStream();
            _ = new ChunkEncodingCustomWriter(writer, -34);
        }

        [Fact]
        public void TestForConstructionErrors1()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ChunkEncodingCustomWriter(null, 1));
        }

        [Fact]
        public async Task TestWriting1()
        {
            // arrange.
            var destStream = new MemoryStream();
            var instance = new ChunkEncodingCustomWriter(destStream);

            var reader = new MemoryStream();

            var expected = new byte[] {
                0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();
            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriting2()
        {
            // arrange.
            var destStream = new MemoryStream();
            int maxChunkSize = 6;
            var instance = new ChunkEncodingCustomWriter(destStream,
                maxChunkSize);

            var srcData = "data bits and bytes";
            // get randomized read request sizes.
            var reader = new RandomizedReadSizeBufferReader(
                ByteUtils.StringToBytes(srcData));

            var expected = new byte[] { 0 ,0, 8, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                0, 0, 8, 1, 0, (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', 0, 0, 8, 1, 0, (byte)'d', (byte)' ',
                (byte)'b', (byte)'y', (byte)'t', (byte)'e', 0, 0, 3, 1, 0,
                (byte)'s', 0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();
            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriting3()
        {
            // arrange.
            var destStream = new MemoryStream();
            var backingWriter = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    destStream.WriteAsync(data, offset, length)
            };
            int maxChunkSize = 9;
            var instance = new ChunkEncodingCustomWriter(backingWriter,
                maxChunkSize);

            var srcData = "data bits and byte";
            // get randomized read request sizes.
            var reader = new RandomizedReadSizeBufferReader(
                ByteUtils.StringToBytes(srcData));

            var expected = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', 0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriting4()
        {
            // arrange.
            var destStream = new MemoryStream();
            var backingWriter = new LambdaBasedCustomReaderWriter
            {
                WriteFunc = (data, offset, length) =>
                    destStream.WriteAsync(data, offset, length)
            };
            int maxChunkSize = 20;
            var instance = new ChunkEncodingCustomWriter(backingWriter,
                maxChunkSize);

            var srcData = "data bits and pieces";
            // get randomized read request sizes.
            var reader = new RandomizedReadSizeBufferReader(
                ByteUtils.StringToBytes(srcData));

            var expected = new byte[] { 0 ,0, 22, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'p',
                (byte)'i', (byte)'e', (byte)'c', (byte)'e', (byte)'s',
                0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestWriting5()
        {
            // arrange.
            var destStream = new MemoryStream();
            int maxChunkSize = -25;
            var instance = new ChunkEncodingCustomWriter(destStream,
                maxChunkSize);

            var srcData = "data bits and places";
            // get randomized read request sizes.
            var reader = new RandomizedReadSizeBufferReader(
                ByteUtils.StringToBytes(srcData));

            var expected = new byte[] { 0 ,0, 22, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'p',
                (byte)'l', (byte)'a', (byte)'c', (byte)'e', (byte)'s',
                0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Test acceptance of hard limit as max chunk size
        /// </summary>
        [Fact]
        public async Task TestWriting6()
        {
            // arrange.
            var destStream = new MemoryStream();
            int maxChunkSize = ChunkedTransferCodec.HardMaxChunkSizeLimit;
            var instance = new ChunkEncodingCustomWriter(destStream,
                maxChunkSize);

            var srcData = "it is finished.";
            // get randomized read request sizes.
            var reader = new RandomizedReadSizeBufferReader(
                ByteUtils.StringToBytes(srcData));

            var expected = new byte[] { 0 ,0, 17, 1, 0, (byte)'i',
                (byte)'t', (byte)' ', (byte)'i', (byte)'s', (byte)' ',
                (byte)'f', (byte)'i', (byte)'n', (byte)'i', (byte)'s',
                (byte)'h', (byte)'e', (byte)'d', (byte)'.',
                0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        /// <summary>
        /// Test hard limit usage for data exceeding default limit.
        /// </summary>
        [Fact]
        public async Task TestWriting7()
        {
            // arrange.
            var destStream = new MemoryStream();
            int maxChunkSize = ChunkedTransferCodec.HardMaxChunkSizeLimit;
            var instance = new ChunkEncodingCustomWriter(destStream,
                maxChunkSize);

            var reader = new MemoryStream();
            reader.Write(ByteUtils.StringToBytes("1".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("2".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("3".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("4".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("5".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("6".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("7".PadRight(10_000)));
            reader.Write(ByteUtils.StringToBytes("8".PadRight(10_000)));
            reader.Position = 0; // reset for reading

            // create expectation
            var expected = new MemoryStream();
            expected.Write(new byte[] { 1, 0x38, 0x82, 1, 0 });
            expected.Write(ByteUtils.StringToBytes("1".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("2".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("3".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("4".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("5".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("6".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("7".PadRight(10_000)));
            expected.Write(ByteUtils.StringToBytes("8".PadRight(10_000)));
            expected.Write(new byte[] { 0, 0, 2, 1, 0 });
            expected.Position = 0; // reset for reading

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected.ToArray(), actual);
        }

        /// <summary>
        /// Test truncation of hard limit excesses to default max chunk size
        /// </summary>
        [Fact]
        public async Task TestWriting8()
        {
            // arrange first with default max chunk size
            var destStream = new MemoryStream();
            int maxChunkSize = 8_192;
            var instance = new ChunkEncodingCustomWriter(destStream,
                maxChunkSize);

            var reader = new MemoryStream();
            reader.Write(ByteUtils.StringToBytes("1".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("2".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("3".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("4".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("5".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("6".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("7".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("8".PadRight(1_000)));
            reader.Write(ByteUtils.StringToBytes("9".PadRight(1_000)));
            reader.Position = 0; // reset for reading

            // create expectation
            var expected = new MemoryStream();
            expected.Write(new byte[] { 0, 0x20, 0x02, 1, 0 });
            expected.Write(ByteUtils.StringToBytes("1".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("2".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("3".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("4".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("5".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("6".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("7".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("8".PadRight(1_000)));
            expected.Write(ByteUtils.StringToBytes("9".PadRight(192)));
            expected.Write(new byte[] { 0, 0x03, 0x2a, 1, 0 });
            expected.Write(ByteUtils.StringToBytes("".PadRight(808)));
            expected.Write(new byte[] { 0, 0, 2, 1, 0 });
            expected.Position = 0; // reset for reading

            // act
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected.ToArray(), actual);

            // try again with too large max chunk size.
            reader.Position = 0; // reset for another reading.
            destStream.SetLength(0); // reset for writing
            instance = new ChunkEncodingCustomWriter(destStream,
                ChunkedTransferCodec.HardMaxChunkSizeLimit + 1);
            await IOUtils.CopyBytes(reader, instance);
            await instance.EndWrites();
            actual = destStream.ToArray();
            Assert.Equal(expected.ToArray(), actual);
        }
    }
}
