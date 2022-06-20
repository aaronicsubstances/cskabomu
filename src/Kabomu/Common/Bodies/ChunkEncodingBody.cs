using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class ChunkEncodingBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;
        private bool _endOfReadSeen;

        public ChunkEncodingBody(IQuasiHttpBody wrappedBody)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentException("null wrapped body");
            }
            _wrappedBody = wrappedBody;
        }

        public long ContentLength => -1;

        public string ContentType => _wrappedBody.ContentType;

        public async Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            var chunkPrefix = new SubsequentChunk
            {
                Version = LeadChunk.Version01
            }.Serialize();
            var chunkPrefixLength = ByteUtils.CalculateSizeOfSlices(chunkPrefix);
            var reservedBytesToUse = 2 + chunkPrefixLength;
            if (bytesToRead <= reservedBytesToUse)
            {
                throw new ArgumentException("invalid bytes to read");
            }
            bytesToRead = Math.Min(bytesToRead, TransportUtils.MaxChunkSize);

            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }
            int bytesRead = await _wrappedBody.ReadBytes(eventLoop, data,
                offset + reservedBytesToUse, bytesToRead - reservedBytesToUse);

            lock (eventLoop)
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
                ByteUtils.SerializeUpToInt64BigEndian(bytesRead + chunkPrefixLength, data, offset, 2);
                int sliceBytesWritten = 0;
                foreach (var slice in chunkPrefix)
                {
                    Array.Copy(slice.Data, slice.Offset, data, offset + 2 + sliceBytesWritten, slice.Length);
                    sliceBytesWritten += slice.Length;
                }
                return bytesRead + reservedBytesToUse;
            }
        }

        public Task EndRead(IEventLoopApi eventLoop, Exception e)
        {
            return _wrappedBody.EndRead(eventLoop, e);
        }
    }
}
