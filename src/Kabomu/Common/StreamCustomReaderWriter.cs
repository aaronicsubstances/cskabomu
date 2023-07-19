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
        private readonly Stream _backingStream;

        // helps with testing, in which memory streams can be disposed
        // such that subsequent reading or writing to them fails
        private readonly CancellationTokenSource _streamCancellationHandle = new CancellationTokenSource();

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
            return _backingStream.ReadAsync(data, offset, length, _streamCancellationHandle.Token);
        }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            return _backingStream.WriteAsync(data, offset, length, _streamCancellationHandle.Token);
        }

        public Task CustomDispose()
        {
            _streamCancellationHandle.Cancel();
#if NETCOREAPP3_1_OR_GREATER
            return _backingStream.DisposeAsync();
#else
            _backingStream.Dispose();
            return Task.CompletedTask;
#endif
        }
    }
}
