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

        public StreamBackedBody(Stream backingStream, long contentLength, string contentType)
        {
            if (backingStream == null)
            {
                throw new ArgumentException("null backing stream");
            }
            BackingStream = backingStream;
            ContentType = contentType ?? TransportUtils.ContentTypeByteStream;
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
        public string ContentType { get; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (bytesToRead == 0 || _bytesRemaining == 0)
            {
                return 0;
            }
            if (_bytesRemaining > 0)
            {
                bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
            }

            // supplying cancellation token is for the purpose of leveraging
            // presence of cancellation in C#'s stream interface. Outside code 
            // should not depend on ability to cancel ongoing reads.
            int bytesRead = await BackingStream.ReadAsync(data, offset, bytesToRead, _readCancellationHandle.Token);

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (_bytesRemaining > 0)
            {
                if (bytesRead == 0)
                {
                    var e = new Exception($"could not read remaining {_bytesRemaining} " +
                        $"bytes before end of read");
                    throw e;
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
