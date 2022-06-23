using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class ChunkEncodingBody : IQuasiHttpBody
    {
        internal static readonly int LengthOfEncodedChunkLength = 3;
        public static readonly int MaxChunkSizeLimit= 1 << (8 * LengthOfEncodedChunkLength) - 1;

        private readonly object _lock = new object();

        private readonly IQuasiHttpBody _wrappedBody;
        private readonly int _maxChunkSize;
        private bool _endOfReadSeen;

        public ChunkEncodingBody(IQuasiHttpBody wrappedBody, int maxChunkSize)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentException("null wrapped body");
            }
            if (maxChunkSize < 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            if (maxChunkSize > MaxChunkSizeLimit)
            {
                throw new ArgumentException($"max chunk size cannot exceed {MaxChunkSizeLimit}. received: {maxChunkSize}");
            }
            _wrappedBody = wrappedBody;
            _maxChunkSize = maxChunkSize;
        }

        public long ContentLength => -1;

        public string ContentType => _wrappedBody.ContentType;

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            var chunkPrefix = new SubsequentChunk
            {
                Version = LeadChunk.Version01
            }.Serialize();
            var chunkPrefixLength = ByteUtils.CalculateSizeOfSlices(chunkPrefix);
            var reservedBytesToUse = LengthOfEncodedChunkLength + chunkPrefixLength;
            if (bytesToRead <= reservedBytesToUse)
            {
                throw new ArgumentException("invalid bytes to read");
            }
            bytesToRead = Math.Min(bytesToRead, _maxChunkSize);
            if (bytesToRead <= reservedBytesToUse)
            {
                throw new ArgumentException("max chunk size too small to read and encode any number of bytes");
            }

            int bytesRead = await _wrappedBody.ReadBytes(data,
                offset + reservedBytesToUse, bytesToRead - reservedBytesToUse);

            lock (_lock)
            {
                if (bytesRead == 0)
                {
                    if (_endOfReadSeen)
                    {
                        return 0;
                    }
                    else
                    {
                        _endOfReadSeen = true;
                    }
                }
                ByteUtils.SerializeUpToInt64BigEndian(bytesRead + chunkPrefixLength, data, offset,
                    LengthOfEncodedChunkLength);
                int sliceBytesWritten = 0;
                foreach (var slice in chunkPrefix)
                {
                    Array.Copy(slice.Data, slice.Offset, 
                        data, offset + LengthOfEncodedChunkLength + sliceBytesWritten, 
                        slice.Length);
                    sliceBytesWritten += slice.Length;
                }
                return bytesRead + reservedBytesToUse;
            }
        }

        public Task EndRead(Exception e)
        {
            return _wrappedBody.EndRead(e);
        }
    }
}
