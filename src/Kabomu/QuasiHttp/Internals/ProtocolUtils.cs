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

        public static void ReadBytesFully(IQuasiHttpTransport transport, IEventLoopApi eventLoop,
            object connection, byte[] data, int offset, int bytesToRead, Action<Exception> finalCb)
        {
            Action<Exception, int> cb = (e, bytesRead) =>
            {
                if (e != null)
                {
                    eventLoop.PostCallback(_ => finalCb.Invoke(e), null);
                }
                else
                {
                    if (bytesRead < bytesToRead)
                    {
                        ReadBytesFully(transport, eventLoop, connection, data, offset + bytesRead, bytesToRead - bytesRead, finalCb);
                    }
                    else
                    {
                        eventLoop.PostCallback(_ => finalCb.Invoke(null), null);
                    }
                }
            };
            transport.ReadBytes(connection, data, offset, bytesToRead, cb);
        }
    }
}