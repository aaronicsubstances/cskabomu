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
            Action<Exception, int> continuationCb = (e, bytesRead) =>
            {
                if (e != null)
                {
                    cb.Invoke(e);
                }
                else
                {
                    if (bytesRead < bytesToRead)
                    {
                        ReadBytesFully(transport, connection, data, offset + bytesRead, bytesToRead - bytesRead, cb);
                    }
                    else
                    {
                        cb.Invoke(null);
                    }
                }
            };
            transport.ReadBytes(connection, data, offset, bytesToRead, continuationCb);
        }

        public static void TransferBodyToTransport(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            bool releaseConnectionOnSuccessfulTransfer)
        {
            byte[] buffer = new byte[8192];
            HandleWriteOutcome(transport, connection, body, releaseConnectionOnSuccessfulTransfer, buffer, null);
        }

        private static void HandleWriteOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            bool releaseConnectionOnSuccessfulTransfer, byte[] buffer, Exception e)
        {
            if (e != null)
            {
                transport.ReleaseConnection(connection);
                body.OnEndRead(e);
                return;
            }
            try
            {
                body.OnDataRead(buffer, 0, buffer.Length, (e, bytesRead) =>
                    HandleReadOutcome(transport, connection, body, releaseConnectionOnSuccessfulTransfer, buffer, e, bytesRead));
            }
            catch (Exception e2)
            {
                transport.ReleaseConnection(connection);
                body.OnEndRead(e2);
            }
        }

        private static void HandleReadOutcome(IQuasiHttpTransport transport, object connection, IQuasiHttpBody body,
            bool releaseConnectionOnSuccessfulTransfer, byte[] buffer, Exception e, int bytesRead)
        {
            if (e != null)
            {
                transport.ReleaseConnection(connection);
                body.OnEndRead(e);
                return;
            }
            if (bytesRead == 0)
            {
                if (releaseConnectionOnSuccessfulTransfer)
                {
                    transport.ReleaseConnection(connection);
                }
                body.OnEndRead(null);
                return;
            }
            try
            {
                transport.WriteBytesOrSendMessage(connection, buffer, 0, bytesRead, e =>
                    HandleWriteOutcome(transport, connection, body, releaseConnectionOnSuccessfulTransfer, buffer, e));
            }
            catch (Exception e2)
            {
                transport.ReleaseConnection(connection);
                body.OnEndRead(e2);
            }
        }
    }
}