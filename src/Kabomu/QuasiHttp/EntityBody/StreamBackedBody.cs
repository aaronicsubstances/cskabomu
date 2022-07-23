using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class StreamBackedBody : IQuasiHttpBody
    {
        private readonly CancellationTokenSource _readCancellationHandle = new CancellationTokenSource();
        private long _bytesRemaining;

        public StreamBackedBody(Stream backingStream):
            this(backingStream, -1)
        {
        }

        public StreamBackedBody(Stream backingStream, long contentLength)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            BackingStream = backingStream;
            ContentLength = contentLength;
            if (ContentLength >= 0)
            {
                _bytesRemaining = contentLength;
            }
            else
            {
                _bytesRemaining = -1;
            }
        }

        public Stream BackingStream { get; }
        public long ContentLength { get; }
        public string ContentType { get; set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (_bytesRemaining >= 0)
            {
                bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
            }
            
            // even if bytes to read is zero at this stage, still go ahead and call
            // wrapped body instead of trying to optimize by returning zero, so that
            // any end of read error can be thrown.

            // supplying cancellation token is for the purpose of leveraging
            // presence of cancellation in C#'s stream interface. Outside code 
            // should not depend on ability to cancel ongoing reads.
            int bytesRead = await BackingStream.ReadAsync(data, offset, bytesToRead, _readCancellationHandle.Token);

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (_bytesRemaining > 0)
            {
                if (bytesRead == 0)
                {
                    throw new ContentLengthNotSatisfiedException(
                        ContentLength,
                        $"could not read remaining {_bytesRemaining} " +
                        $"bytes before end of read", null);
                }
                _bytesRemaining -= bytesRead;
            }
            return bytesRead;
        }

        public async Task EndRead()
        {
            if (_readCancellationHandle.IsCancellationRequested)
            {
                return;
            }

            _readCancellationHandle.Cancel();
            // assume that a stream can be disposed concurrently with any ongoing use of it.
#if NETCOREAPP3_1
            await BackingStream.DisposeAsync();
#else
            BackingStream.Dispose();
#endif
        }
    }
}
