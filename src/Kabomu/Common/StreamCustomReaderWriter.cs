using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    /// <summary>
    /// Represents stream of bytes directly with an instance of the <see cref="Stream"/> class.
    /// </summary>
    public class StreamCustomReaderWriter : ICustomReader, ICustomWriter
    {
        private readonly CancellationTokenSource _streamCancellationHandle = new CancellationTokenSource();
        private readonly Stream _backingStream;

        /// <summary>
        /// Creates an instance with an input stream which will supply bytes to be read
        /// </summary>
        /// <param name="backingStream">the input stream</param
        public StreamCustomReaderWriter(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            _backingStream = backingStream;
        }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            // supplying cancellation token is for the purpose of leveraging
            // presence of cancellation in C#'s stream interface. Outside code 
            // should not depend on ability to cancel ongoing reads.
            return _backingStream.ReadAsync(data, offset, length, _streamCancellationHandle.Token);
        }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            return _backingStream.WriteAsync(data, offset, length, _streamCancellationHandle.Token);
        }

        public async Task CustomDispose()
        {
            _streamCancellationHandle.Cancel();
            // assume that a stream can be disposed concurrently with any ongoing use of it.
#if NETCOREAPP3_1
            await _backingStream.DisposeAsync();
#else
            _backingStream.Dispose();
#endif
        }
    }
}
