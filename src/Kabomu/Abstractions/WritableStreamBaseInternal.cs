using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Convenient base class for clients to implement custom writable streams.
    /// </summary>
    /// <remarks>
    /// The only required methods to implement are
    /// <see cref="Stream.WriteAsync(byte[], int, int, CancellationToken)"/>
    /// <see cref="Stream.Write(byte[], int, int)"/>,
    /// <see cref="Stream.FlushAsync(System.Threading.CancellationToken)"/>, and
    /// <see cref="Stream.Flush"/>.
    /// Optionally one can also override <see cref="Stream.WriteByte(byte)"/> for
    /// efficiency gains.
    /// </remarks>
    public abstract class WritableStreamBaseInternal : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

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

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(
            byte[] buffer, int offset, int count,
            AsyncCallback callback, object state)
        {
            return WriteAsync(buffer, offset, count).AsApm(callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            try
            {
                ((Task)asyncResult).Wait();
            }
            catch (AggregateException e)
            {
                if (e.InnerExceptions.Count == 1)
                {
                    throw e.InnerException;
                }
                throw;
            }
        }
    }
}
