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
        public void TestWriteByteSlices()
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
            var slices = new ByteBufferSlice[]
            {
                new ByteBufferSlice
                {
                    Data = new byte[]{ 0 },
                    Length = 1
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 0, 2, 1 },
                    Offset = 1,
                    Length = 2
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 7, 8, 9, 10 },
                    Length = 3
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 7, 8, 9, 10 },
                    Length = 0
                },
                new ByteBufferSlice
                {
                    Data = new byte[]{ 7, 8, 9, 10, 11 },
                    Offset = 3,
                    Length = 1
                }
            };
            var expectedStreamContents = new byte[] { 0, 7, 0, 2, 1, 7, 8, 9, 10 };

            // act.
            var cbCalled = false;
            ProtocolUtils.WriteByteSlices(transport, connection, slices, e =>
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
        public void TestWriteByteSlicesForArgumentErrors()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                ProtocolUtils.WriteByteSlices(null, null, new ByteBufferSlice[0], e => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ProtocolUtils.WriteByteSlices(new ConfigurableQuasiHttpTransport(), null, null, e => { });
            });
            Assert.Throws<ArgumentException>(() =>
            {
                ProtocolUtils.WriteByteSlices(new ConfigurableQuasiHttpTransport(), null, new ByteBufferSlice[0], null);
            });
            Assert.ThrowsAny<Exception>(() =>
            {
                ProtocolUtils.WriteByteSlices(new ConfigurableQuasiHttpTransport(), null, new ByteBufferSlice[] { null },
                    e => { });
            });
        }
    }
}
