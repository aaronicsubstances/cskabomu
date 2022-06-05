using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Common
{
    public static class TransportUtils
    {
        public static void ReadBytesFully(IQuasiHttpTransport transport,
            object connection, byte[] data, int offset, int bytesToRead, Action<Exception> cb)
        {
            transport.ReadBytes(connection, data, offset, bytesToRead, (e, bytesRead) =>
                HandlePartialReadOutcome(transport, connection, data, offset, bytesToRead, e, bytesRead, cb));
        }

        private static void HandlePartialReadOutcome(IQuasiHttpTransport transport,
           object connection, byte[] data, int offset, int bytesToRead, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead < bytesToRead)
            {
                if (bytesRead <= 0)
                {
                    cb.Invoke(new Exception("end of read"));
                    return;
                }
                int newOffset = offset + bytesRead;
                int newBytesToRead = bytesToRead - bytesRead;
                transport.ReadBytes(connection, data, newOffset, newBytesToRead, (e, bytesRead) =>
                   HandlePartialReadOutcome(transport, connection, data, newOffset, newBytesToRead, e, bytesRead, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        public static void TransferBodyToTransport(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, Action<Exception> cb)
        {
            int effectiveChunkSize = transport.MaxChunkSize;
            if (effectiveChunkSize <= 0)
            {
                effectiveChunkSize = 3;
            }
            effectiveChunkSize = Math.Min(effectiveChunkSize, 65_535); // max unsigned 16-bit integer.
            byte[] buffer = new byte[effectiveChunkSize];
            body.OnDataRead(mutex, buffer, 2, buffer.Length - 2, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, mutex, buffer, e, bytesRead, cb));
        }

        private static void HandleReadOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, byte[] buffer, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead < 0)
            {
                bytesRead = 0;
            }
            ByteUtils.SerializeUpToInt64BigEndian(bytesRead, buffer, 0, 2);
            transport.WriteBytes(connection, buffer, 0, bytesRead + 2, e =>
                HandleWriteOutcome(transport, connection, body, mutex, buffer, e, bytesRead, cb));
        }

        private static void HandleWriteOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, byte[] buffer, Exception e, int bytesWritten, Action<Exception> cb)
        {
            if (e != null || bytesWritten == 0)
            {
                cb.Invoke(e);
                return;
            }
            body.OnDataRead(mutex, buffer, 2, buffer.Length - 2, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, mutex, buffer, e, bytesRead, cb));
        }

        public static void ReadBodyToEnd(IQuasiHttpBody body, IMutexApi mutex, int maxChunkSize,
            Action<Exception, byte[]> cb)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();
            body.OnDataRead(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
                HandleReadOutcome2(body, mutex, readBuffer, byteStream, e, i, cb));
        }

        private static void HandleReadOutcome2(IQuasiHttpBody body, IMutexApi mutex, byte[] readBuffer,
            MemoryStream byteStream, Exception e, int bytesRead, Action<Exception, byte[]> cb)
        {
            if (e != null)
            {
                cb.Invoke(e, null);
                return;
            }
            if (bytesRead > 0)
            {
                byteStream.Write(readBuffer, 0, bytesRead);
                body.OnDataRead(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
                    HandleReadOutcome2(body, mutex, readBuffer, byteStream, e, i, cb));
            }
            else
            {
                body.OnEndRead(mutex, null);
                cb.Invoke(null, byteStream.ToArray());
            }
        }
    }
}
