using Kabomu.Common;
using Kabomu.QuasiHttp;
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
    public class ChunkDecodingCustomReaderTest
    {
        [Fact]
        public void TestForConstructionSuccess()
        {
            var reader = new MemoryStream();
            _ = new ChunkDecodingCustomReader(reader);
        }

        [Fact]
        public void TestForConstructionErrors()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ChunkDecodingCustomReader(null));
        }

        [Fact]
        public async Task TestReading1()
        {
            // arrange
            var srcData = new byte[] { 0, 0, 2, 1, 0 };
            var backingReader = new MemoryStream(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();
            var expected = "";

            // act
            await IOUtils.CopyBytes(instance, writer);

            // assert
            Assert.Equal(expected, MiscUtils.BytesToString(
                writer.ToArray()));

            // ensure subsequent reading attempts return 0
            Assert.Equal(0, await instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestReading2()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 22, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'p',
                (byte)'l', (byte)'a', (byte)'c', (byte)'e', (byte)'s',
                0, 0, 2, 1, 0
            };
            var backingReader = new MemoryStream(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();
            var expected = "data bits and places";

            // act
            await IOUtils.CopyBytes(instance, writer);

            // assert
            Assert.Equal(expected, MiscUtils.BytesToString(
                writer.ToArray()));

            // ensure subsequent reading attempts return 0
            Assert.Equal(0, await instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestReading3()
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
            var backingReader = new RandomizedReadSizeBufferReader(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();
            var expected = "data bits and bytes";

            // act
            await IOUtils.CopyBytes(instance, writer);

            // assert
            Assert.Equal(expected, MiscUtils.BytesToString(
                writer.ToArray()));

            // ensure subsequent reading attempts return 0
            Assert.Equal(0, await instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestReading4()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i', (byte)'t', (byte)'s', 0, 0, 11, 1, 0, (byte)' ',
                (byte)'a', (byte)'n', (byte)'d', (byte)' ', (byte)'b',
                (byte)'y', (byte)'t', (byte)'e', 0, 0, 2, 1, 0
            };
            // get randomized read request sizes.
            var backingReader = new RandomizedReadSizeBufferReader(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();
            var expected = "data bits and byte";

            // act
            await IOUtils.CopyBytes(instance, writer);

            // assert
            Assert.Equal(expected, MiscUtils.BytesToString(
                writer.ToArray()));

            // ensure subsequent reading attempts return 0
            Assert.Equal(0, await instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestReading5()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 17, 1, 0, (byte)'i',
                (byte)'t', (byte)' ', (byte)'i', (byte)'s', (byte)' ',
                (byte)'f', (byte)'i', (byte)'n', (byte)'i', (byte)'s',
                (byte)'h', (byte)'e', (byte)'d', (byte)'.',
                0, 0, 2, 1, 0
            };
            var backingReader = new MemoryStream(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();
            var expected = "it is finished.";

            // act
            await IOUtils.CopyBytes(instance, writer);

            // assert
            Assert.Equal(expected, MiscUtils.BytesToString(
                writer.ToArray()));

            // ensure subsequent reading attempts return 0
            Assert.Equal(0, await instance.ReadBytes(new byte[1], 0, 1));
        }

        [Fact]
        public async Task TestReadingError1()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 11, 1 };
            var backingReader = new MemoryStream(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();

            // act and assert
            var actualEx = await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                IOUtils.CopyBytes(instance, writer));
            Assert.Contains("quasi http body", actualEx.Message);
        }

        [Fact]
        public async Task TestReadingError2()
        {
            // arrange
            var srcData = new byte[] { 0 ,0, 11, 1, 0, (byte)'d',
                (byte)'a', (byte)'t', (byte)'a', (byte)' ', (byte)'b',
                (byte)'i'};
            var backingReader = new MemoryStream(srcData);
            var instance = new ChunkDecodingCustomReader(
                backingReader);
            var writer = new MemoryStream();

            // act and assert
            var actualEx = await Assert.ThrowsAsync<ChunkDecodingException>(() =>
                IOUtils.CopyBytes(instance, writer));
            Assert.Contains("quasi http body", actualEx.Message);
        }
    }
}
