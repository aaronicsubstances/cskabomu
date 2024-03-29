﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Abstractions
{
    /// <summary>
    /// Convenient base class for clients to implement custom readable streams.
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

        public override IAsyncResult BeginRead(
            byte[] buffer, int offset, int count,
            AsyncCallback callback, object state)
        {
            return ReadAsync(buffer, offset, count).AsApm(callback, state);
        }


        public override int EndRead(IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<int>)asyncResult).Result;
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
