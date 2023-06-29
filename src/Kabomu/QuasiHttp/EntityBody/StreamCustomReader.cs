using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class StreamCustomReader : ICustomReader, ICustomWritable<IDictionary<string, object>>
    {
        private readonly CancellationTokenSource _streamCancellationHandle = new CancellationTokenSource();

        /// <summary>
        /// Creates an instance with an input stream which will supply bytes to be read
        /// </summary>
        /// <param name="backingStream"></param>
        public StreamCustomReader(Stream backingStream)
        {
            if (backingStream == null)
            {
                throw new ArgumentNullException(nameof(backingStream));
            }
            BackingStream = backingStream;
        }

        /// <summary>
        /// Returns the stream backing this instance.
        /// </summary>
        public Stream BackingStream { get; }

        public Task<int> ReadAsync(byte[] data, int offset, int length)
        {
            // supplying cancellation token is for the purpose of leveraging
            // presence of cancellation in C#'s stream interface. Outside code 
            // should not depend on ability to cancel ongoing reads.
            return BackingStream.ReadAsync(data, offset, length, _streamCancellationHandle.Token);
        }

        public Task WriteToAsync(ICustomWriter writer, IDictionary<string, object> context)
        {
            return BackingStream.CopyToAsync(new AsyncWriterWrapper(writer));
        }

        public async Task CloseAsync()
        {
            _streamCancellationHandle.Cancel();
            // assume that a stream can be disposed concurrently with any ongoing use of it.
#if NETCOREAPP3_1
            await BackingStream.DisposeAsync();
#else
            BackingStream.Dispose();
#endif
        }

        class AsyncWriterWrapper : Stream
        {
            private readonly ICustomWriter _backingWriter;

            public AsyncWriterWrapper(ICustomWriter backingWriter)
            {
                _backingWriter = backingWriter;
            }

            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => -1;

            public override long Position { get; set; }

            public override void Flush()
            {
                // called by FlushAsync()
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return _backingWriter.WriteAsync(buffer, offset, count);
            }
        }
    }
}
