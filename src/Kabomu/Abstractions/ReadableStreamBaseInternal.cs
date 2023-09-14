using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Convenient base class for clients to implement custom Streams.
    /// </summary>
    /// <remarks>
    /// The only required methods to implement are
    /// <see cref="Stream.ReadAsync(byte[], int, int, CancellationToken)"/> and
    /// <see cref="Stream.Read(byte[], int, int)"/>.
    /// Optionally one can also override <see cref="Stream.ReadByte"/> for
    /// efficiency gains when reading from an internal buffer.
    /// </remarks>
    internal abstract class ReadableStreamBaseInternal : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async Task CopyToAsync(Stream destination, int bufferSize,
            CancellationToken cancellationToken)
        {
            await IOUtilsInternal.CopyBytesToSink(this,
                (data, offset, length) =>
                    destination.WriteAsync(data, offset, length, cancellationToken),
                bufferSize,
                cancellationToken);
        }

        public override IAsyncResult BeginRead(
            byte[] buffer, int offset, int count,
            AsyncCallback callback, object state)
        {
            return ReadAsync(buffer, offset, count).AsApm(callback, state);
        }


        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).Result;
        }
    }
}
