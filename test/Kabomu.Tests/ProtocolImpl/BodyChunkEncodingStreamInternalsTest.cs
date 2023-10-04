using Kabomu.ProtocolImpl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.ProtocolImpl
{
    public class BodyChunkEncodingStreamInternalsTest
    {
        /// <summary>
        /// NB: Test method only tests with one and zero,
        /// so as to guarantee that data will not be split,
        /// even when test is ported to other languages.
        /// </summary>
        [Fact]
        public async Task TestWriting()
        {
            // arrange
            byte srcByte = 45;
            var tagToUse = 16;
            var expected = new byte[]
            {
                0, 0, 0, 16,
                0, 0, 0, 1,
                45,
                0, 0, 0, 16,
                0, 0, 0, 0
            };
            var destStream = new MemoryStream();
            var instance = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            await instance.WriteAsync(new byte[] { srcByte });
            await instance.WriteAsync(new byte[0]);
            // write end of stream
            await instance.WriteAsync(null, 0, -1);

            // assert
            var actual = destStream.ToArray();
            Assert.Equal(expected, actual);

            // test with sync

            // arrange
            srcByte = 145;
            tagToUse = 0x79452316;
            expected = new byte[]
            {
                0x79, 0x45, 0x23, 0x16,
                0, 0, 0, 1,
                145,
                0x79, 0x45, 0x23, 0x16,
                0, 0, 0, 0
            };
            destStream = new MemoryStream();
            instance = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            instance.Write(new byte[] { srcByte });
            instance.Write(new byte[0]);
            // write end of stream
            instance.Write(null, 0, -1);

            // assert
            actual = destStream.ToArray();
            Assert.Equal(expected, actual);

            // test with slow sync

            // arrange
            srcByte = 78;
            tagToUse = 0x3cd456;
            expected = new byte[]
            {
                0, 0x3c, 0xd4, 0x56,
                0, 0, 0, 1,
                78
            };
            destStream = new MemoryStream();
            instance = TlvUtils.CreateTlvEncodingWritableStream(
                destStream, tagToUse);

            // act
            instance.WriteByte(srcByte);

            // assert
            actual = destStream.ToArray();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task TestForCancellation()
        {
            // 1. arrange
            var stream = new MemoryStream();
            var instance = TlvUtils.CreateTlvEncodingWritableStream(stream,
                5);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await instance.WriteAsync(new byte[2], cts.Token));
        }
    }
}
