using Kabomu.Internals;
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
            int effectiveChunkSize = Math.Min(transport.MaxChunkSize, MaxChunkSize);
            byte[] buffer = new byte[effectiveChunkSize];
            body.ReadBytes(mutex, buffer, 0, buffer.Length, (e, bytesRead) =>
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
            if (bytesRead > 0)
            {
                transport.WriteBytes(connection, buffer, 0, bytesRead, e =>
                    HandleWriteOutcome(transport, connection, body, mutex, buffer, e, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }

        private static void HandleWriteOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            IMutexApi mutex, byte[] buffer, Exception e, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            body.ReadBytes(mutex, buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, mutex, buffer, e, bytesRead, cb));
        }

        public static void ReadBodyToEnd(IQuasiHttpBody body, IMutexApi mutex, int maxChunkSize,
            Action<Exception, byte[]> cb)
        {
            var readBuffer = new byte[maxChunkSize];
            var byteStream = new MemoryStream();
            body.ReadBytes(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
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
                body.ReadBytes(mutex, readBuffer, 0, readBuffer.Length, (e, i) =>
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
