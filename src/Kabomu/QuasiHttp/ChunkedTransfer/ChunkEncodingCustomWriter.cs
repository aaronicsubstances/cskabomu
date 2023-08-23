using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.ChunkedTransfer
{
    /// <summary>
    /// The standard chunk encoder of byte streams in the Kabomu library. Receives a writer
    /// into which it writes an unknown number of one or more chunks, 
    /// in which the last chunk has zero data length
    /// and all the previous ones have non-empty data. The last zero-data chunk
    /// is written only when <see cref="EndWrites"/> method is called.
    /// </summary>
    public class ChunkEncodingCustomWriter : ICustomWriter
    {
        private readonly object _wrappedWriter;
        private readonly byte[] _buffer;
        private readonly ChunkedTransferCodec _chunkTransferUtils = new ChunkedTransferCodec();
        private int _usedBufferOffset;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="wrappedWriter">the backing writer through which
        /// the encoded bytes will be sent. Must be acceptable by
        /// <see cref="IOUtils.WriteBytes"/> function.</param>
        /// <param name="maxChunkSize">maximum size of chunks. Can pass
        /// 0 to use a default value.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="wrappedWriter"/> is null</exception>
        /// <exception cref="ArgumentException">If <paramref name="maxChunkSize"/> exceeds
        /// the maximum signed 24-bit integer.</exception>
        public ChunkEncodingCustomWriter(object wrappedWriter,
            int maxChunkSize = 0)
        {
            if (wrappedWriter == null)
            {
                throw new ArgumentNullException(nameof(wrappedWriter));
            }
            if (maxChunkSize <= 0 || maxChunkSize > ChunkedTransferCodec.HardMaxChunkSizeLimit)
            {
                maxChunkSize = ChunkedTransferCodec.DefaultMaxChunkSize;
            }
            _wrappedWriter = wrappedWriter;
            _buffer = new byte[maxChunkSize];
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
                await _chunkTransferUtils.EncodeSubsequentChunkV1Header(
                    _buffer.Length, _wrappedWriter);

                // next empty buffer
                await IOUtils.WriteBytes(_wrappedWriter, _buffer, 0, _usedBufferOffset);
                _usedBufferOffset = 0;

                // now directly transfer data to writer.
                await IOUtils.WriteBytes(_wrappedWriter, data, offset, chunkRem);
            }
            return chunkRem;
        }

        public async Task EndWrites()
        {
            // write out remaining data.
            if (_usedBufferOffset > 0)
            {
                await _chunkTransferUtils.EncodeSubsequentChunkV1Header(
                    _usedBufferOffset, _wrappedWriter);
                await IOUtils.WriteBytes(_wrappedWriter, _buffer, 0, _usedBufferOffset);
                _usedBufferOffset = 0;
            }

            // end by writing out empty terminating chunk
            await _chunkTransferUtils.EncodeSubsequentChunkV1Header(
                0, _wrappedWriter);
        }
    }
}
