using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents stream of bytes directly with an instance of the <see cref="Stream"/> class.
    /// </summary>
    public class StreamCustomReader : ICustomReader, ICustomWritable<IDictionary<string, object>>
    {
        private readonly CancellationTokenSource _streamCancellationHandle = new CancellationTokenSource();
        private readonly int _bufferSize;

        /// <summary>
        /// Creates an instance with an input stream which will supply bytes to be read
        /// </summary>
        /// <param name="backingStream">the input stream</param>
        /// <param name="bufferSize">size of buffer used during transfer to a writer</param>
        public StreamCustomReader(Stream backingStream, int bufferSize)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            BackingStream = backingStream;
            _bufferSize = bufferSize;
        }

        /// <summary>
        /// Returns the stream backing this instance.
        /// </summary>
        public Stream BackingStream { get; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            // supplying cancellation token is for the purpose of leveraging
            // presence of cancellation in C#'s stream interface. Outside code 
            // should not depend on ability to cancel ongoing reads.
            return BackingStream.ReadAsync(data, offset, length, _streamCancellationHandle.Token);
        }

        public Task WriteBytesTo(ICustomWriter writer, IDictionary<string, object> context)
        {
            return TransportUtils.CopyBytes(this, writer, _bufferSize);
        }

        public async Task CustomDispose()
        {
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
