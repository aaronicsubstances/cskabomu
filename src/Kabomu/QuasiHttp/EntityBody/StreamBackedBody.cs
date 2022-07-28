using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents stream of bytes directly with an instance of the <see cref="Stream"/> class.
    /// </summary>
    public class StreamBackedBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private readonly CancellationTokenSource _streamCancellationHandle = new CancellationTokenSource();
        private long _bytesRemaining;

        /// <summary>
        /// Creates an instance with an input stream in which all of its bytes should be returned in reads (content
        /// length will be -1).
        /// </summary>
        /// <param name="backingStream">the input stream to read from</param>
        public StreamBackedBody(Stream backingStream):
            this(backingStream, -1)
        {
        }

        /// <summary>
        /// Creates an input stream with an input stream and the number of bytes to read.
        /// </summary>
        /// <param name="backingStream"></param>
        /// <param name="contentLength">the total number of bytes to read from stream. can be -1 (actually any
        /// negative value) to indicate that all bytes of stream should be returned.</param>
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

        /// <summary>
        /// Returns the stream backing this instance.
        /// </summary>
        public Stream BackingStream { get; }

        /// <summary>
        /// Returns the number of bytes to read, or negative value to indicate that all
        /// bytes of backing stream are to be read.
        /// </summary>
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
            int bytesRead = await BackingStream.ReadAsync(data, offset, bytesToRead, _streamCancellationHandle.Token);

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
            if (!_readCancellationHandle.Cancel())
            {
                return;
            }

            _streamCancellationHandle.Cancel();
            // assume that a stream can be disposed concurrently with any ongoing use of it.
#if NETCOREAPP3_1
            await BackingStream.DisposeAsync();
#else
            BackingStream.Dispose();
#endif
        }
    }
}
