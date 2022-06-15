using Kabomu.Common;
using System;

namespace Kabomu.QuasiHttp
{
    internal class ProtocolUtils
    {
        public static void WriteLeadChunk(IQuasiHttpTransport transport, object connection, 
            LeadChunk chunk, Action<Exception> cb)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (chunk == null)
            {
                throw new ArgumentException("null chunk");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            var slices = chunk.Serialize();
            WriteSizeOfSlices(transport, connection, slices, cb);
        }

        private static void WriteSizeOfSlices(IQuasiHttpTransport transport, object connection,
            ByteBufferSlice[] slices, Action<Exception> cb)
        {
            int byteCount = 0;
            foreach (var slice in slices)
            {
                byteCount += slice.Length;
            }
            if (byteCount > transport.MaxChunkSize)
            {
                cb.Invoke(new Exception("larger than max chunk size of transport"));
                return;
            }
            if (byteCount > TransportUtils.MaxChunkSize)
            {
                cb.Invoke(new Exception("size cannot fit in 16-bit unsigned integer"));
                return;
            }
            var encodedLength = new byte[2];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            transport.WriteBytes(connection, encodedLength, 0, encodedLength.Length, e =>
            {
                if (e != null)
                {
                    cb.Invoke(e);
                    return;
                }
                TransportUtils.WriteByteSlices(transport, connection, slices, cb);
            });
        }
    }
}