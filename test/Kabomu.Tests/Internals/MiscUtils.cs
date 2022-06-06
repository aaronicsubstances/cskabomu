﻿using Kabomu.Common;
using Kabomu.Tests.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public static class MiscUtils
    {

        public static byte[] ReadChunkedBody(byte[] data, int offset, int length)
        {
            var inputStream = new MemoryStream();
            inputStream.Write(data, offset, length);
            inputStream.Position = 0; // rewind.
            var transport = new ConfigurableQuasiHttpTransport
            {
                ReadBytesCallback = (connection, data, offset, length, cb) =>
                {
                    int bytesRead = inputStream.Read(data, offset, length);
                    cb.Invoke(null, bytesRead);
                }
            };
            var body = new ChunkDecodingBody(null, transport, null, null);
            byte[] result = null;
            TransportUtils.ReadBodyToEnd(body, new TestEventLoopApi(), 100, (e, d) =>
            {
                Assert.Null(e);
                result = d;
            });
            Assert.NotNull(result);
            return result;
        }
    }
}