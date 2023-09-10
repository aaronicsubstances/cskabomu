using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Impl
{
    public abstract class ReadableStreamBase : Stream
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
            await MiscUtils.CopyBytesToSink(this,
                (data, offset, length) =>
                    destination.WriteAsync(data, offset, length, cancellationToken),
                bufferSize,
                cancellationToken);
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count),
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
