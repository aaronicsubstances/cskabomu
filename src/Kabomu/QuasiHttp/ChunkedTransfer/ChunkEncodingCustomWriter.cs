using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk encoder of byte streams in the Kabomu library. Receives a writer
    /// into which it writes an unknown number of one or more chunks, 
    /// in which the last chunk has zero data length
    /// and all the previous ones have non-empty data. The last zero-data chunk
    /// is written only when an instance of this class is disposed.
    /// </summary>
    public class ChunkEncodingCustomWriter : ICustomWriter
    {
        private readonly ICustomWriter _wrappedWriter;
        private readonly byte[] _buffer;
        private readonly byte[] _chunkHeaderBuffer;
        private int _usedBufferOffset;

        public ChunkEncodingCustomWriter(ICustomWriter wrappedWriter,
            int maxChunkSize)
        {
            if (wrappedWriter == null)
            {
                throw new ArgumentNullException(nameof(wrappedWriter));
            }
            if (maxChunkSize <= 0)
            {
                maxChunkSize = ChunkedTransferUtils.DefaultMaxChunkSize;
            }
            if (maxChunkSize > ChunkedTransferUtils.HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"max chunk size cannot exceed {ChunkedTransferUtils.HardMaxChunkSizeLimit}. received: {maxChunkSize}");
            }
            _wrappedWriter = wrappedWriter;
            _buffer = new byte[maxChunkSize];
            _chunkHeaderBuffer = new byte[10];
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
            if (_usedBufferOffset + chunkRem < _buffer.Length)
            {
                // save data in buffer for later sending
                Array.Copy(data, offset, _buffer, _usedBufferOffset,
                    chunkRem);
                _usedBufferOffset += chunkRem;
            }
            else
            {
                await ChunkedTransferUtils.EncodeSubsequentChunkHeader(
                    _buffer.Length, _wrappedWriter, _chunkHeaderBuffer);

                // next empty buffer
                await _wrappedWriter.WriteBytes(_buffer, 0, _usedBufferOffset);
                _usedBufferOffset = 0;

                // now directly transfer data to writer.
                await _wrappedWriter.WriteBytes(data, offset, chunkRem);
            }
            return chunkRem;
        }

        public async Task CustomDispose()
        {
            // flush out remaining data.
            if (_usedBufferOffset > 0)
            {
                await ChunkedTransferUtils.EncodeSubsequentChunkHeader(
                    _usedBufferOffset, _wrappedWriter, _chunkHeaderBuffer);
                await _wrappedWriter.WriteBytes(_buffer, 0, _usedBufferOffset);
                _usedBufferOffset = 0;
            }

            // end by writing out empty terminating chunk
            await ChunkedTransferUtils.EncodeSubsequentChunkHeader(0, _wrappedWriter,
                _chunkHeaderBuffer);

            await _wrappedWriter.CustomDispose();
        }
    }
}
