using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class ChunkDecodingBody : IQuasiHttpBody
    {
        private readonly object _lock = new object();

        private readonly IQuasiHttpBody _wrappedBody;
        private readonly int _maxChunkSize;
        private readonly Func<Task> _closeCallback;
        private SubsequentChunk _lastChunk;
        private int _lastChunkUsedBytes;
        private Exception _srcEndError;

        public ChunkDecodingBody(IQuasiHttpBody wrappedBody, int maxChunkSize, Func<Task> closeCallback)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentException("null wrapped body");
            }
            if (maxChunkSize < 0)
            {
                throw new ArgumentException("max chunk size must be positive. received: " + maxChunkSize);
            }
            _wrappedBody = wrappedBody;
            _maxChunkSize = maxChunkSize;
            _closeCallback = closeCallback;
        }

        public long ContentLength => -1;

        public string ContentType => _wrappedBody.ContentType;

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task readTask;
            var encodedLength = new byte[ChunkEncodingBody.LengthOfEncodedChunkLength];
            lock (_lock)
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
                readTask = TransportUtils.ReadBytesFully(_wrappedBody,
                    encodedLength, 0, encodedLength.Length);
            }

            await readTask;

            byte[] chunkBytes;
            lock (_lock)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }

                var chunkLen = (int)ByteUtils.DeserializeUpToInt64BigEndian(encodedLength, 0,
                    encodedLength.Length);
                if (chunkLen > TransportUtils.DefaultMaxChunkSizeLimit && chunkLen > _maxChunkSize)
                {
                    throw new Exception(
                        $"received chunk size of {chunkLen} which is both larger than" +
                        $" default limit on max chunk size ({TransportUtils.DefaultMaxChunkSizeLimit})" +
                        $" and maximum configured chunk size of {_maxChunkSize}");
                }
                chunkBytes = new byte[chunkLen];
                readTask = TransportUtils.ReadBytesFully(_wrappedBody,
                    chunkBytes, 0, chunkBytes.Length);
            }

            await readTask;

            lock (_lock)
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

        public async Task EndRead(Exception e)
        {
            Task closeCbTask = null;
            lock (_lock)
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
