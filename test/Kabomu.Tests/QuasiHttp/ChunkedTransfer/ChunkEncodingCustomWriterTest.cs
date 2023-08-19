﻿using Kabomu.Common;
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
        public async Task TestWriting1()
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
        public async Task TestWriting2()
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

            var expected1 = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b', 
                (byte)'y', (byte)'t', (byte)'e'
            };
            var expected2 = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', 0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance);

            // assert without EndWrites()
            var actual = destStream.ToArray();
            Assert.Equal(expected1, actual);

            await instance.EndWrites();

            // assert final stream contents
            actual = destStream.ToArray();
            Assert.Equal(expected2, actual);
        }
    }
}
