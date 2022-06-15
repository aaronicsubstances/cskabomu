using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<int> ReadBytesAsync(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            if (eventLoop.IsMutexRequired(out Task mt)) await mt;

            if (_srcEndError != null)
            {
                throw _srcEndError;
            }

            try
            {
                int bytesRead = await eventLoop.MutexWrap(BackingStream.ReadAsync(data, offset, bytesToRead));
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                return bytesRead;
            }
            catch (Exception e)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                await EndReadInternally(e);
                throw;
            }
        }

        public async Task EndReadAsync(IEventLoopApi eventLoop, Exception e)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null event loop");
            }

            if (eventLoop.IsMutexRequired(out Task mt)) await mt;

            if (_srcEndError != null)
            {
                return;
            }
            await EndReadInternally(e);
        }

        private async Task EndReadInternally(Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            try
            {
                await BackingStream.DisposeAsync();
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
