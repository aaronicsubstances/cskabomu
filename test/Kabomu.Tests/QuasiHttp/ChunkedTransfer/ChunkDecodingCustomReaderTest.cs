﻿using Kabomu.Common;
using Kabomu.QuasiHttp.ChunkedTransfer;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp.ChunkedTransfer
{
    public class ChunkDecodingCustomReaderTest
    {
        [Fact]
        public async Task TestReading1()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 8, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                0, 0, 8, 1, 0, (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', 0, 0, 8, 1, 0, (byte)'d', (byte)' ',
                (byte)'b', (byte)'y', (byte)'t', (byte)'e', 0, 0, 3, 1, 0,
                (byte)'s', 0, 0, 2, 1, 0
            };
            // get randomized read request sizes.
            var backingReader = new DemoCustomReaderWriter(srcData);
            int maxChunkSize = 6;
            var instance = new ChunkDecodingCustomReader(
                backingReader, maxChunkSize);
            var writer = new DemoCustomReaderWriter();
            var expected = "data bits and bytes";

            // act
            await IOUtils.CopyBytes(instance, writer, 2);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.BufferStream.ToArray()));

            // assert disposal of backingReader
            await instance.CustomDispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                backingReader.ReadBytes(new byte[0], 0, 0));
        }

        [Fact]
        public async Task TestReading2()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', 0, 0, 2, 1, 0
            };
            // get randomized read request sizes.
            var backingReader = new DemoCustomReaderWriter(srcData);
            int maxChunkSize = 9;
            var instance = new ChunkDecodingCustomReader(
                backingReader, maxChunkSize);
            var writer = new DemoCustomReaderWriter();
            var expected = "data bits and byte";

            // act
            await IOUtils.CopyBytes(instance, writer, 5);

            // assert
            Assert.Equal(expected, ByteUtils.BytesToString(
                writer.BufferStream.ToArray()));

            // ensure subsequent reading attempts return 0
            Assert.Equal(0, await instance.ReadBytes(new byte[1], 0, 1));

            // assert disposal of backingReader
            await instance.CustomDispose();
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                backingReader.ReadBytes(new byte[0], 0, 0));
        }
    }
}
