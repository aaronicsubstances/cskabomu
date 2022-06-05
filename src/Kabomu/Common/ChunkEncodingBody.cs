using Kabomu.Internals;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public class ChunkEncodingBody : IQuasiHttpBody
    {
        private readonly IQuasiHttpBody _wrappedBody;

        public ChunkEncodingBody(IQuasiHttpBody wrappedBody)
        {
            if (wrappedBody == null)
            {
                throw new ArgumentException("null wrapped body");
            }
            _wrappedBody = wrappedBody;
        }

        public string ContentType => _wrappedBody.ContentType;

        public void OnDataRead(IMutexApi mutex, byte[] data, int offset, int bytesToRead, Action<Exception, int> cb)
        {
            var chunkPrefix = new SubsequentChunk
            {
                Version = LeadChunk.Version01
            }.Serialize()[0];
            var reservedBytesToUse = 2 + chunkPrefix.Length;
            if (bytesToRead <= reservedBytesToUse || bytesToRead >= (1 << 16))
            {
                throw new ArgumentException("invalid bytes to read");
            }
            _wrappedBody.OnDataRead(mutex, data, offset + reservedBytesToUse,
                bytesToRead - reservedBytesToUse, (e, bytesRead) =>
            {
                if (e != null)
                {
                    cb.Invoke(e, 0);
                    return;
                }
                ByteUtils.SerializeUpToInt64BigEndian(bytesRead + chunkPrefix.Length, data, offset, 2);
                Array.Copy(chunkPrefix.Data, chunkPrefix.Offset, data, offset + 2, chunkPrefix.Length);
                cb.Invoke(null, bytesRead);
            });
        }

        public void OnEndRead(IMutexApi mutex, Exception e)
        {
            _wrappedBody.OnEndRead(mutex, e);
        }
    }
}
