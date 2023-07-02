using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    public class ChunkEncodingCustomWriter : ICustomWriter
    {
        private readonly ICustomWriter _wrappedWriter;
        private readonly int _maxChunkSize;
        private readonly byte[] _buffer;
        private int _usedBufferOffset;
        private bool _disposed;

        public ChunkEncodingCustomWriter(ICustomWriter wrappedWriter,
            int maxChunkSize)
        {
            if (wrappedWriter == null)
            {
                throw new ArgumentNullException(nameof(wrappedWriter));
            }
            if (maxChunkSize <= 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            if (maxChunkSize > ChunkedTransferUtils.HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"max chunk size cannot exceed {ChunkedTransferUtils.HardMaxChunkSizeLimit}. received: {maxChunkSize}");
            }
            _wrappedWriter = wrappedWriter;
            _maxChunkSize = maxChunkSize;
            _buffer = new byte[maxChunkSize + ChunkedTransferUtils.ReservedBytesToUse];
            _usedBufferOffset = ChunkedTransferUtils.ReservedBytesToUse;
        }

        public async Task WriteBytes(byte[] data, int offset, int length)
        {
            int bytesWritten = 0;
            while (bytesWritten < length)
            {
                bytesWritten += await WriteNextSubsequentChunk(data,
                    offset + bytesWritten, length - bytesWritten);
            }
        }

        private async Task<int> WriteNextSubsequentChunk(
            byte[] data, int offset, int length)
        {
            int chunkRem = Math.Min(length, _buffer.Length - _usedBufferOffset);
            _usedBufferOffset += chunkRem;
            if (_usedBufferOffset < _buffer.Length)
            {
                // save data in buffer for later sending
                Array.Copy(data, offset, _buffer, _usedBufferOffset - chunkRem,
                    chunkRem);
            }
            else
            {
                // use the first 5 or so bytes to encode the
                // chunk header
                ChunkedTransferUtils.EncodeSubsequentChunkHeader(
                    _maxChunkSize, _buffer, 0);

                // next empty buffer
                await _wrappedWriter.WriteBytes(_buffer, 0,
                    _usedBufferOffset - chunkRem);
                _usedBufferOffset = ChunkedTransferUtils.ReservedBytesToUse;

                // now directly transfer data to writer.
                await _wrappedWriter.WriteBytes(data, offset, chunkRem);
            }
            var leftOver = Math.Max(0, length - chunkRem);
            return leftOver;
        }

        public async Task CustomDispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // flush out remaining data.
            if (_usedBufferOffset > ChunkedTransferUtils.ReservedBytesToUse)
            {
                ChunkedTransferUtils.EncodeSubsequentChunkHeader(
                    _usedBufferOffset - ChunkedTransferUtils.ReservedBytesToUse,
                    _buffer, 0);
                await _wrappedWriter.WriteBytes(_buffer, 0, _usedBufferOffset);
                _usedBufferOffset = ChunkedTransferUtils.ReservedBytesToUse;
            }

            // end by writing out empty terminating chunk
            ChunkedTransferUtils.EncodeSubsequentChunkHeader(0, _buffer, 0);
            await _wrappedWriter.WriteBytes(_buffer, 0, _usedBufferOffset);

            await _wrappedWriter.CustomDispose();
        }
    }
}
