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
    public class ProtocolUtilsTest
    {
        [Fact]
        public void TestWriteLeadChunk()
        {
            // arrange.
            object connection = "dk";
            var destStream = new MemoryStream();
            var transport = new ConfigurableQuasiHttpTransport
            {
                MaxChunkSize = 100,
                WriteBytesCallback = (actualConnection, data, offset, length, cb) =>
                {
                    Assert.Equal(connection, actualConnection);
                    destStream.Write(data, offset, length);
                    cb.Invoke(null);
                }
            };
            var leadChunk = new LeadChunk();
            var leadChunkSlices = leadChunk.Serialize();
            var expectedStreamContents = new byte[2 + leadChunkSlices[0].Length + leadChunkSlices[1].Length];
            expectedStreamContents[1] = (byte)(leadChunkSlices[0].Length + leadChunkSlices[1].Length);
            Array.Copy(leadChunkSlices[0].Data, leadChunkSlices[0].Offset, expectedStreamContents,
                2, leadChunkSlices[0].Length);
            Array.Copy(leadChunkSlices[1].Data, leadChunkSlices[1].Offset, expectedStreamContents,
                2 + leadChunkSlices[0].Length, leadChunkSlices[1].Length);

            // act.
            var cbCalled = false;
            ProtocolUtils.WriteLeadChunk(transport, connection, leadChunk, e =>
            {
                Assert.False(cbCalled);
                Assert.Null(e);
                cbCalled = true;
            });

            // assert.
            Assert.True(cbCalled);
            Assert.Equal(expectedStreamContents, destStream.ToArray());
        }

        [Fact]
        public void TestWriteLeadChunkForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                ProtocolUtils.WriteLeadChunk(null, null, new LeadChunk(), e => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ProtocolUtils.WriteLeadChunk(new ConfigurableQuasiHttpTransport(), null, null, e => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ProtocolUtils.WriteLeadChunk(new ConfigurableQuasiHttpTransport(), null, new LeadChunk(), null);
            });
        }
    }
}
