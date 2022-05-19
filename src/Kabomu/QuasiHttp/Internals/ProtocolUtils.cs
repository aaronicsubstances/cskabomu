using Kabomu.Common;
using System;

namespace Kabomu.QuasiHttp.Internals
{
    internal class ProtocolUtils
    {
        public static bool IsOperationPending(STCancellationIndicator cancellationIndicator)
        {
            return cancellationIndicator != null && !cancellationIndicator.Cancelled;
        }

        public static void ReadBytesFully(IQuasiHttpTransport transport,
            object connection, byte[] data, int offset, int bytesToRead, Action<Exception> cb)
        {
            HandlePartialReadOutcome(transport, connection, data, offset, bytesToRead, null, 0, cb);
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
            Action<Exception> cb)
        {
            byte[] buffer = new byte[8192];
            HandleWriteOutcome(transport, connection, body, buffer, null, cb);
        }

        private static void HandleWriteOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            byte[] buffer, Exception e, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            body.OnDataRead(buffer, 0, buffer.Length, (e, bytesRead) =>
                HandleReadOutcome(transport, connection, body, buffer, e, bytesRead, cb));
        }

        private static void HandleReadOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            byte[] buffer, Exception e, int bytesRead, Action<Exception> cb)
        {
            if (e != null)
            {
                cb.Invoke(e);
                return;
            }
            if (bytesRead > 0)
            {
                transport.WriteBytesOrSendMessage(connection, buffer, 0, bytesRead, e =>
                    HandleWriteOutcome(transport, connection, body, buffer, e, cb));
            }
            else
            {
                cb.Invoke(null);
            }
        }
    }
}