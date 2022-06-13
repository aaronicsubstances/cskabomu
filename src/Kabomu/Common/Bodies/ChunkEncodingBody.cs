using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;

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

        public void ReadBytes(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
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
            _wrappedBody.ReadBytes(mutex, data, offset + reservedBytesToUse,
                bytesToRead - reservedBytesToUse, (e, bytesRead) =>
                {
                    if (e != null)
                    {
                        cb.Invoke(e, 0);
                        return;
                    }
                    if (bytesRead == 0)
                    {
                        if (_endOfReadSeen)
                        {
                            cb.Invoke(null, 0);
                            return;
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
                    cb.Invoke(null, bytesRead + reservedBytesToUse);
                });
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            _wrappedBody.OnEndRead(mutex, e);
        }
    }
}
