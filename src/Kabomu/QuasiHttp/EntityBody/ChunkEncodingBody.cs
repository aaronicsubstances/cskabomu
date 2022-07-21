using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class ChunkEncodingBody : IQuasiHttpBody
    {
        internal static readonly int LengthOfEncodedChunkLength = 3;
        public static readonly int HardMaxChunkSizeLimit = 1 << (8 * LengthOfEncodedChunkLength) - 1;

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
            if (maxChunkSize > HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"max chunk size cannot exceed {HardMaxChunkSizeLimit}. received: {maxChunkSize}");
            }
            _wrappedBody = wrappedBody;
            _maxChunkSize = maxChunkSize;
        }

        public long ContentLength => -1;
        public string ContentType => _wrappedBody.ContentType;

        public static async Task WriteLeadChunk(IQuasiHttpTransport transport, object connection,
            int maxChunkSize, LeadChunk chunk)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            if (chunk == null)
            {
                throw new ArgumentException("null chunk");
            }
            var slices = chunk.Serialize();
            int byteCount = 0;
            foreach (var slice in slices)
            {
                byteCount += slice.Length;
            }
            if (byteCount > maxChunkSize)
            {
                throw new ArgumentException($"headers larger than max chunk size of {maxChunkSize}");
            }
            if (byteCount > HardMaxChunkSizeLimit)
            {
                throw new ArgumentException($"headers larger than max chunk size limit of {HardMaxChunkSizeLimit}");
            }
            var encodedLength = new byte[LengthOfEncodedChunkLength];
            ByteUtils.SerializeUpToInt64BigEndian(byteCount, encodedLength, 0,
                encodedLength.Length);
            await transport.WriteBytes(connection, encodedLength, 0, encodedLength.Length);
            await TransportUtils.WriteByteSlices(transport, connection, slices);
        }

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
                if (bytesToRead < 0)
                {
                    throw new ArgumentException("invalid bytes to read");
                }
                else
                {
                    throw new ArgumentException($"require at least {reservedBytesToUse + 1} bytes to read");
                }
            }
            bytesToRead = Math.Min(bytesToRead, _maxChunkSize);
            if (bytesToRead <= reservedBytesToUse)
            {
                throw new ArgumentException("max chunk size too small to read and encode any number of bytes");
            }

            int bytesRead = await _wrappedBody.ReadBytes(data,
                offset + reservedBytesToUse, bytesToRead - reservedBytesToUse);

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

        public Task EndRead()
        {
            return _wrappedBody.EndRead();
        }
    }
}
