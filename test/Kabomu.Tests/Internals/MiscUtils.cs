using Kabomu.Common;
using Kabomu.Common.Bodies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Kabomu.Tests.Internals
{
    public static class MiscUtils
    {
        public static void WriteChunk(ByteBufferSlice[] slices, Action<byte[], int, int> writeCallback)
        {
            var byteCount = (short)ByteUtils.CalculateSizeOfSlices(slices);
            var encodedLength = ByteUtils.SerializeInt16BigEndian(byteCount);
            writeCallback.Invoke(encodedLength, 0, encodedLength.Length);
            foreach (var slice in slices)
            {
                writeCallback.Invoke(slice.Data, slice.Offset, slice.Length);
            }
        }

        public static Task<byte[]> ReadChunkedBody(byte[] data, int offset, int length)
        {
            var body = new ChunkDecodingBody(new ByteBufferBody(data, offset, length, null), 100, null);
            return TransportUtils.ReadBodyToEnd(body, 100);
        }
    }
}
