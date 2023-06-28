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
    /// <summary>
    /// Represents stream of bytes directly with an instance of the <see cref="Stream"/> class.
    /// </summary>
    public class StreamBackedBody : IQuasiHttpBody, IBytesAlreadyReadProviderInternal
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private readonly CancellationTokenSource _streamCancellationHandle = new CancellationTokenSource();

        /// <summary>
        /// Creates an instance with an input stream which will supply bytes to be read
        /// </summary>
        /// <param name="backingStream"></param>
        public StreamBackedBody(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            BackingStream = backingStream;
            ContentLength = -1;
        }

        /// <summary>
        /// Returns the stream backing this instance.
        /// </summary>
        public Stream BackingStream { get; }

        /// <summary>
        /// Returns the number of bytes to read, or negative value to indicate that all
        /// bytes of backing stream are to be read.
        /// </summary>
        public long ContentLength { get; set; }

        public string ContentType { get; set; }

        long IBytesAlreadyReadProviderInternal.BytesAlreadyRead { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            async Task<int> ReadBytesInternal(int bytesToRead)
            {
                // supplying cancellation token is for the purpose of leveraging
                // presence of cancellation in C#'s stream interface. Outside code 
                // should not depend on ability to cancel ongoing reads.
                int bytesRead = await BackingStream.ReadAsync(data, offset, bytesToRead, _streamCancellationHandle.Token);
                
                EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);
                
                return bytesRead;
            }

            return EntityBodyUtilsInternal.PerformGeneralRead(this,
                length, ReadBytesInternal);
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
