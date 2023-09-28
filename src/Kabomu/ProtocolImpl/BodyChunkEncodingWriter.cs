using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
    /// <summary>
    /// The standard encoder of quasi http body chunks on the fly
    /// in accordance with the quasi web protocol
    /// defined in the Kabomu library, to any destination
    /// represented by a function which consumes byte chunks.
    /// </summary>
    public class BodyChunkEncodingWriter
    {
        /// <summary>
        /// Writes out quasi body chunk that represents the end
        /// of a quasi body stream.
        /// </summary>
        /// <param name="sink">destination of writes</param>
        /// <returns>a task representing asynchronous operation</returns>
        public async Task WriteEnd(
            Func<byte[], int, int, Task> sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }
            var encodingBuffer = TlvUtils.EncodeTagLengthOnly(
                QuasiHttpCodec.TagForBody, 0);
            await sink(encodingBuffer, 0, encodingBuffer.Length);
        }

        /// <summary>
        /// Chops up a byte buffer into quasi body chunks and
        /// writes them out.
        /// </summary>
        /// <param name="data">byte buffer to be encoded and written
        /// out as zero or more quasi http body chunks</param>
        /// <param name="sink">destination of writes</param>
        /// <returns>a task representing asynchronous operation</returns>
        public Task WriteData(byte[] data,
            Func<byte[], int, int, Task> sink)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }
            return EncodeBodyChunkV1(data, 0, data.Length, sink);
        }

        /// <summary>
        /// Chops up a byte buffer slice into quasi body chunks and
        /// writes them out.
        /// </summary>
        /// <param name="data">byte buffer from which a portion will
        /// be encoded and written out as zero or more quasi http body
        /// chunks</param>
        /// <param name="offset">starting position of byte buffer portion</param>
        /// <param name="length">number of bytes of byte buffer slice</param>
        /// <param name="sink">destination of writes</param>
        /// <returns>a task representing asynchronous operation</returns>
        public async Task WriteData(
            byte[] data, int offset, int length,
            Func<byte[], int, int, Task> sink)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (sink == null)
            {
                throw new ArgumentNullException(nameof(sink));
            }
            if (!MiscUtilsInternal.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid byte buffer slice");
            }
            await EncodeBodyChunkV1(data, offset, length, sink);
        }

        /// <summary>
        /// This separation is meant to enable testing huge lengths
        /// without actually allocating byte arrays.
        /// In other words validation of byte buffer slice is skipped.
        /// </summary>
        internal async Task EncodeBodyChunkV1(
            byte[] data, int offset, int length,
            Func<byte[], int, int, Task> sink)
        {
            int endOffset = offset + length;
            while (offset < endOffset)
            {
                int nextChunkLength = Math.Min(endOffset - offset,
                    TlvUtils.MaxAllowableTagValueLength);
                var encodingBuffer = TlvUtils.EncodeTagLengthOnly(
                    QuasiHttpCodec.TagForBody, nextChunkLength);
                await sink(encodingBuffer, 0, encodingBuffer.Length);
                await sink(data, offset, nextChunkLength);
                offset += nextChunkLength;
            }
        }
    }
}
