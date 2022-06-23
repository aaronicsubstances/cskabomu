using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.QuasiHttp
{
    public class ProtocolUtilsInternalTest
    {
        [Fact]
        public async Task TestWriteLeadChunk()
        {
            // arrange.
            object connection = "dk";
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                WriteBytesCallback = (actualConnection, data, offset, length) =>
                {
                    Assert.Equal(connection, actualConnection);
                    destStream.Write(data, offset, length);
                    return Task.CompletedTask;
                }
            };
            var leadChunk = new LeadChunk();
            var leadChunkSlices = leadChunk.Serialize();
            var lengthOfEncodedChunkLength = 3;
            var expectedStreamContents = new byte[lengthOfEncodedChunkLength + leadChunkSlices[0].Length + leadChunkSlices[1].Length];
            expectedStreamContents[2] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            Array.Copy(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, expectedStreamContents,
                lengthOfEncodedChunkLength, leadChunkSlices[0].Length);
            Array.Copy(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, expectedStreamContents,
                lengthOfEncodedChunkLength + leadChunkSlices[0].Length, leadChunkSlices[1].Length);

            // act.
            await ProtocolUtils.WriteLeadChunk(transport, connection, 1000, leadChunk);

            // assert.
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public async Task TestWriteLeadChunkForArgumentErrors()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return ProtocolUtils.WriteLeadChunk(null, null, 100, new LeadChunk());
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return ProtocolUtils.WriteLeadChunk(new ConfigurableQuasiHttpTransport(), null, 100, null);
            });
            await Assert.ThrowsAsync<ArgumentException>(() =>
            {
                return ProtocolUtils.WriteLeadChunk(new ConfigurableQuasiHttpTransport(), null, 1, new LeadChunk());
            });
        }
    }
}
