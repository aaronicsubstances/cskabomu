using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kabomu.Common
{
    public static class TransportUtils
    {
        public static readonly int MaxChunkSize = 65_535; // ie max unsigned 16-bit integer value.

        public static readonly string ContentTypePlainText = "text/plain";
        public static readonly string ContentTypeByteStream = "application/octet-stream";
        public static readonly string ContentTypeJson = "application/json";
        public static readonly string ContentTypeHtmlFormUrlEncoded = "application/x-www-form-urlencoded";

        public static void ReadBytesFully(IMutexApi mutex, IQuasiHttpBody body,
            byte[] data, int offset, int bytesToRead, Action<Exception> cb)
        {
            body.ReadBytes(mutex, data, offset, bytesToRead, (e, bytesRead) =>
                HandlePartialReadOutcome(mutex, body, data, offset, bytesToRead, e, bytesRead, cb));
        }

        private static void HandlePartialReadOutcome(IMutexApi mutex, IQuasiHttpBody body,
           byte[] data, int offset, int bytesToRead, Exception e, int bytesRead, Action<Exception> cb)
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
                body.ReadBytes(mutex, data, newOffset, newBytesToRead, (e, bytesRead) =>
                   HandlePartialReadOutcome(mutex, body, data, newOffset, newBytesToRead, e, bytesRead, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        public static void TransferBodyToTransport(IMutexApi mutex, IQuasiHttpTransport transport, 
            object connection, IQuasiHttpBody body, Action<Exception> cb)
        {
            int effectiveChunkSize = Math.Min(transport.MaxChunkSize, MaxChunkSize);
            byte[] buffer = new byte[effectiveChunkSize];
            body.ReadBytes(mutex, buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(mutex, transport, connection, body, buffer, e, bytesRead, cb));
        }

        private static void HandleReadOutcome(IMutexApi mutex, IQuasiHttpTransport transport, object connection, 
            IQuasiHttpBody body, byte[] buffer, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead > 0)
            {
                transport.WriteBytes(connection, buffer, 0, bytesRead, e =>
                    HandleWriteOutcome(mutex, transport, connection, body, buffer, e, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        private static void HandleWriteOutcome(IMutexApi mutex, IQuasiHttpTransport transport, object connection,
            IQuasiHttpBody body, byte[] buffer, Exception e, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            body.ReadBytes(mutex, buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(mutex, transport, connection, body, buffer, e, bytesRead, cb));
        }

        public static void ReadBodyToEnd(IMutexApi mutex, IQuasiHttpBody body, int maxChunkSize,
            Action<Exception, byte[]> cb)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();
            body.ReadBytes(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
                HandleReadBodyToEndOutcome(mutex, body, readBuffer, byteStream, e, i, cb));
        }

        private static void HandleReadBodyToEndOutcome(IMutexApi mutex, IQuasiHttpBody body, byte[] readBuffer,
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
                body.ReadBytes(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
                    HandleReadBodyToEndOutcome(mutex, body, readBuffer, byteStream, e, i, cb));
            }
            else
            {
                body.OnEndRead(mutex, null);
                cb.Invoke(null, byteStream.ToArray());
            }
        }

        public static void WriteByteSlices(IQuasiHttpTransport transport, object connection,
            ByteBufferSlice[] slices, Action<Exception> cb)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (slices == null)
            {
                throw new ArgumentException("null byte slices");
            }
            if (cb == null)
            {
                throw new ArgumentException("null callback");
            }
            WriteSlice(transport, connection, slices, 0, cb);
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
            transport.WriteBytes(connection, nextSlice.Data, nextSlice.Offset, nextSlice.Length, e =>
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
