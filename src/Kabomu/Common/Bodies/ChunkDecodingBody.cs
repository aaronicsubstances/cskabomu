using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class ChunkDecodingBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;
        private readonly Func<Task> _closeCallback;
        private SubsequentChunk _lastChunk;
        private int _lastChunkUsedBytes;
        private Exception _srcEndError;

        public ChunkDecodingBody(IQuasiHttpBody wrappedBody, Func<Task> closeCallback)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentException("null wrapped body");
            }
            _wrappedBody = wrappedBody;
            _closeCallback = closeCallback;
        }

        public long ContentLength => -1;

        public string ContentType => _wrappedBody.ContentType;

        public async Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null mutex api");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task readTask;
            var encodedLength = new byte[2];
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }

                // once empty data chunk is seen, return 0 for all subsequent reads.
                if (_lastChunk != null && (_lastChunk.DataLength == 0 || _lastChunkUsedBytes < _lastChunk.DataLength))
                {
                    return SupplyFromLastChunk(data, offset, bytesToRead);
                }
                readTask = TransportUtils.ReadBytesFully(eventLoop, _wrappedBody,
                    encodedLength, 0, encodedLength.Length);
            }

            await readTask;

            byte[] chunkBytes;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }

                var chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                    encodedLength.Length);
                chunkBytes = new byte[chunkLen];
                readTask = TransportUtils.ReadBytesFully(eventLoop, _wrappedBody,
                    chunkBytes, 0, chunkBytes.Length);
            }

            await readTask;

            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }

                _lastChunk = SubsequentChunk.Deserialize(chunkBytes, 0, chunkBytes.Length);
                _lastChunkUsedBytes = 0;
                return SupplyFromLastChunk(data, offset, bytesToRead);
            }
        }

        private int SupplyFromLastChunk(byte[] data, int offset, int bytesToRead)
        {
            int lengthToUse = Math.Min(_lastChunk.DataLength - _lastChunkUsedBytes, bytesToRead);
            Array.Copy(_lastChunk.Data, _lastChunk.DataOffset + _lastChunkUsedBytes, data, offset, lengthToUse);
            _lastChunkUsedBytes += lengthToUse;
            return lengthToUse;
        }

        public async Task EndRead(IEventLoopApi eventLoop, Exception e)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }

            Task closeCbTask = null;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = e ?? new Exception("end of read");
                if (_closeCallback != null)
                {
                    closeCbTask = _closeCallback.Invoke();
                }
            }

            if (closeCbTask != null)
            {
                await closeCbTask;
            }
        }
    }
}
