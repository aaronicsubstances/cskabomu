using Kabomu.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.ProtocolImpl
{
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
            V1HeaderPrefix = MiscUtils.StringToBytes(
                QuasiHttpCodec.ProtocolVersion01 + ",");
        }

        public BodyChunkEncodingWriter()
        {
            _encodingBuffer = new byte[V1HeaderPrefix.Length +
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

        public async Task GenerateEndChunk(
            Func<byte[], int, int, Task> sink)
        {
            EncodeBodyChunkV1Header(0, _encodingBuffer);
            await sink(_encodingBuffer, 0, _encodingBuffer.Length);
        }

        public Task GenerateDataChunks(byte[] data,
            Func<byte[], int, int, Task> sink)
        {
            return GenerateDataChunks(data, 0, data.Length, sink);
        }

        public async Task GenerateDataChunks(
            byte[] data, int offset, int length,
            Func<byte[], int, int, Task> sink)
        {
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
