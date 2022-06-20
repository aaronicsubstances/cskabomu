using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class StreamBackedBody : IQuasiHttpBody
    {
        private Exception _srcEndError;

        public StreamBackedBody(Stream backingStream, string contentType)
        {
            if (backingStream == null)
            {
                throw new ArgumentException("null backing stream");
            }
            BackingStream = backingStream;
            ContentType = contentType ?? TransportUtils.ContentTypeByteStream;
        }

        public long ContentLength => -1;

        public string ContentType { get; }

        public Stream BackingStream { get; }

        public async Task<int> ReadBytes(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            Task<int> readTask;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                readTask = BackingStream.ReadAsync(data, offset, bytesToRead);
            }

            int bytesRead = await readTask;
            return bytesRead;
        }

        public async Task EndRead(IEventLoopApi eventLoop, Exception e)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }

            ValueTask disposeTask;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    return;
                }

                _srcEndError = e ?? new Exception("end of read");
                disposeTask = BackingStream.DisposeAsync();
            }

            await disposeTask;
        }
    }
}
