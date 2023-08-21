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
        public void TestForConstructionErrors2()
        {
            Assert.Throws<ArgumentException>(() =>
                new ChunkEncodingCustomWriter(new MemoryStream(),
                    10_000_000));
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
    }
}
