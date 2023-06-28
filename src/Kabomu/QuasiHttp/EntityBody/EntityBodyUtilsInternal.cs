using Kabomu.Common;
using Kabomu.QuasiHttp.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    internal static class EntityBodyUtilsInternal
    {
        public static void ThrowIfReadCancelled(ICancellationHandle cancellationHandle)
        {
            ThrowIfReadCancelled(cancellationHandle.IsCancelled);
        }

        public static void ThrowIfReadCancelled(bool endOfReadSeen)
        {
            if (endOfReadSeen)
            {
                throw new EndOfReadException();
            }
        }

        public static async Task<int> PerformGeneralRead(IQuasiHttpBody body,
            int bytesToRead, Func<int, Task<int>> customReadFunc)
        {
            var bytesAlreadyReadSrc = (IBytesAlreadyReadProviderInternal)body;
            long maxBytesToRead = body.ContentLength;
            if (maxBytesToRead >= 0)
            {
                maxBytesToRead = body.ContentLength;

                // just in case content length is changed in between reads,
                // ensure bytesToRead will never become negative.
                bytesToRead = (int)Math.Max(0,
                    Math.Min(maxBytesToRead - bytesAlreadyReadSrc.BytesAlreadyRead,
                    bytesToRead));
            }

            // even if bytes to read is zero at this stage, still go ahead and call
            // wrapped body instead of trying to optimize by returning zero, so that
            // any end of read error can be thrown.
            int bytesJustRead = await customReadFunc.Invoke(bytesToRead);
            
            // update record of number of bytes read.
            bytesAlreadyReadSrc.BytesAlreadyRead += bytesJustRead;

            // if end of read is encountered, ensure that all
            // requested bytes have been read.
            var remainingBytesToRead = maxBytesToRead - bytesAlreadyReadSrc.BytesAlreadyRead;
            if (bytesJustRead == 0 && remainingBytesToRead > 0)
            {
                throw new ContentLengthNotSatisfiedException(
                    body.ContentLength,
                    $"could not read remaining {remainingBytesToRead} " +
                    $"bytes before end of read", null);
            }
            return bytesJustRead;
        }
    }
}
