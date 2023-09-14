using Kabomu.Exceptions;
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
        /// The number of ASCII bytes which indicate the length of
        /// data in a body chunk.
        /// </summary>
        internal const int LengthOfEncodedBodyChunkLength = 10;

        /// <summary>
        /// The maximum allowable body chunk data length.
        /// </summary>
        private const int MaxBodyChunkLength = 2_000_000_000;

        private static readonly byte[] V1HeaderPrefix;
        private readonly byte[] _encodingBuffer;

        static BodyChunkEncodingWriter()
        {
            V1HeaderPrefix = MiscUtilsInternal.StringToBytes(
                QuasiHttpCodec.ProtocolVersion01 + ",");
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public BodyChunkEncodingWriter()
        {
            _encodingBuffer = AllocateBodyChunkHeaderBuffer();
        }

        internal static byte[] AllocateBodyChunkHeaderBuffer()
        {
            return new byte[V1HeaderPrefix.Length +
                LengthOfEncodedBodyChunkLength];
        }

        internal static int EncodeBodyChunkV1Header(int length,
            byte[] sink, int offset = 0)
        {
            if (length < 0)
            {
                throw new ExpectationViolationException(
                    $"length argument is negative: {length}");
            }
            Array.Copy(V1HeaderPrefix, 0, sink, offset, V1HeaderPrefix.Length);
            int absStartOffset = V1HeaderPrefix.Length;
            int absEndOffset = V1HeaderPrefix.Length + LengthOfEncodedBodyChunkLength - 1;
            for (int i = absEndOffset; i >= absStartOffset; i--)
            {
                var digitAsAscii = 48 + (length % 10); // 48 is ascii for 0
                sink[offset + i] = (byte)digitAsAscii;
                length /= 10;
            }
            return V1HeaderPrefix.Length + LengthOfEncodedBodyChunkLength;
        }

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
            EncodeBodyChunkV1Header(0, _encodingBuffer);
            await sink(_encodingBuffer, 0, _encodingBuffer.Length);
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
            return WriteData(data, 0, data.Length, sink);
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
            int endOffset = offset + length;
            while (offset < endOffset)
            {
                int nextChunkLength = Math.Min(endOffset - offset,
                    MaxBodyChunkLength);
                EncodeBodyChunkV1Header(nextChunkLength, _encodingBuffer);
                await sink(_encodingBuffer, 0, _encodingBuffer.Length);
                await sink(data, offset, nextChunkLength);
                offset += nextChunkLength;
            }
        }
    }
}
