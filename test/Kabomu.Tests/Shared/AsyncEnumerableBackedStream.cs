using Kabomu.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu
{
    public class AsyncEnumerableBackedStream : ReadableStreamBase
    {
        private byte[] _outstandingChunk;
        private int _usedOffset;
        private readonly IAsyncEnumerator<byte[]> _backingAsyncEnumerator;

        public AsyncEnumerableBackedStream(IAsyncEnumerable<byte[]> backingGenerator)
        {
            if (backingGenerator == null)
            {
                throw new ArgumentNullException(nameof(backingGenerator));
            }
            _backingAsyncEnumerator = backingGenerator.GetAsyncEnumerator();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public override int ReadByte()
        {
            if (_outstandingChunk != null &&
                _usedOffset < _outstandingChunk.Length)
            {
                var b = _outstandingChunk[_usedOffset];
                _usedOffset++;
                return b;
            }
            return base.ReadByte();
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_outstandingChunk != null &&
                _usedOffset < _outstandingChunk.Length)
            {
                return FillFromOutstanding(buffer);
            }
            while (await _backingAsyncEnumerator.MoveNextAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var chunk = _backingAsyncEnumerator.Current;
                // ignore empty chunks
                if (chunk.Length > 0)
                {
                    _outstandingChunk = chunk;
                    _usedOffset = 0;
                    return FillFromOutstanding(buffer);
                }
            }
            return 0;
        }

        private int FillFromOutstanding(Memory<byte> buffer)
        {
            var nextChunkLength = Math.Min(buffer.Length,
                _outstandingChunk.Length - _usedOffset);
            var span = buffer.Span;
            for (int i = 0; i < nextChunkLength; i++)
            {
                span[i] = _outstandingChunk[_usedOffset];
                _usedOffset++;
            }
            return nextChunkLength;
        }
    }
}
