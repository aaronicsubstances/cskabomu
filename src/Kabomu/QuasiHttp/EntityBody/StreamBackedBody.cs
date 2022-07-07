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

        public StreamBackedBody(Stream backingStream, string contentType)
        {
            if (backingStream == null)
            {
                throw new ArgumentException("null backing stream");
            }
            BackingStream = backingStream;
            ContentType = contentType ?? TransportUtils.ContentTypeByteStream;
        }

        public long ContentLength => -1;

        public string ContentType { get; }

        public Stream BackingStream { get; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            // supplying cancellation token is for the purpose of leveraging
            // presence of cancellation in C#'s stream interface. Outside code 
            // should not depend on ability to cancel ongoing reads.
            int bytesRead = await BackingStream.ReadAsync(data, offset, bytesToRead, _readCancellationHandle.Token);
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
            await BackingStream.DisposeAsync();
        }
    }
}
