using Kabomu.Common;
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
    public class ChunkEncodingCustomWriterTest
    {
        [Fact]
        public async Task TestWriting1()
        {
            // arrange.
            var destStream = new MemoryStream();
            var backingWriter = new StreamCustomReaderWriter(destStream);
            int maxChunkSize = 6;
            var instance = new ChunkEncodingCustomWriter(backingWriter,
                maxChunkSize);

            var srcData = "data bits and bytes";
            // get randomized read request sizes.
            var reader = new DemoCustomReaderWritable(
                Encoding.UTF8.GetBytes(srcData));

            var expected = new byte[] { 0 ,0, 8, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                0, 0, 8, 1, 0, (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', 0, 0, 8, 1, 0, (byte)'d', (byte)' ',
                (byte)'b', (byte)'y', (byte)'t', (byte)'e', 0, 0, 3, 1, 0,
                (byte)'s', 0, 0, 2, 1, 0
            };

            // act
            await IOUtils.CopyBytes(reader, instance, 2);
            await instance.CustomDispose();

            // assert backing writer was disposed
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                backingWriter.WriteBytes(new byte[0], 0, 0));

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }
        [Fact]
        public async Task TestWriting2()
        {
            // arrange.
            var destStream = new MemoryStream();
            var backingWriter = new StreamCustomReaderWriter(destStream);
            int maxChunkSize = 9;
            var instance = new ChunkEncodingCustomWriter(backingWriter,
                maxChunkSize);

            var srcData = "data bits and byte";
            // get randomized read request sizes.
            var reader = new DemoCustomReaderWritable(
                Encoding.UTF8.GetBytes(srcData));

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
            await IOUtils.CopyBytes(reader, instance, 5);

            // assert without dispose
            var actual = destStream.ToArray();
            Assert.Equal(expected1, actual);

            // assert backing writer was disposed
            await instance.CustomDispose();
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                backingWriter.WriteBytes(new byte[0], 0, 0));

            // assert final stream contents
            actual = destStream.ToArray();
            Assert.Equal(expected2, actual);
        }
    }
}
