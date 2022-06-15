using Kabomu.Common;
using System;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ProtocolUtils
    {
        public static Task WriteLeadChunkAsync(IQuasiHttpTransport transport, object connection, 
            LeadChunk chunk)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (chunk == null)
            {
                throw new ArgumentException("null chunk");
            }
            var slices = chunk.Serialize();
            return WriteSizeOfSlicesAsync(transport, connection, slices);
        }

        private static async Task WriteSizeOfSlicesAsync(IQuasiHttpTransport transport, object connection,
            ByteBufferSlice[] slices)
        {
            int byteCount = 0;
            foreach (var slice in slices)
            {
                byteCount += slice.Length;
            }
            if (byteCount > transport.MaxChunkSize)
            {
                throw new Exception("larger than max chunk size of transport");
            }
            if (byteCount > TransportUtils.MaxChunkSize)
            {
                throw new Exception("size cannot fit in 16-bit unsigned integer");
            }
            var encodedLength = new byte[2];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await transport.WriteBytesAsync(connection, encodedLength, 0, encodedLength.Length);
            await TransportUtils.WriteByteSlicesAsync(transport, connection, slices);
        }
    }
}