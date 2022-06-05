using Kabomu.Common;
using System;

namespace Kabomu.Internals
{
    internal class ProtocolUtils
    {
        public static void WriteBytes(IQuasiHttpTransport transport, object connection, 
            ByteBufferSlice[] slices, Action<Exception> cb)
        {
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
                WriteSlice(transport, connection, slices, 0, cb);
            });
        }

        private static void WriteSlice(IQuasiHttpTransport transport, object connection, 
            ByteBufferSlice[] slices, int index, Action<Exception> cb)
        {
            if (index >= slices.Length)
            {
                cb.Invoke(null);
                return;
            }
            var nextSlice = slices[index];
            transport.WriteBytes(connection, nextSlice.Data, nextSlice.Offset, slices.Length, e =>
            {
                if (e != null)
                {
                    cb.Invoke(e);
                    return;
                }
                WriteSlice(transport, connection, slices, index + 1, cb);
            });
        }
    }
}