using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Bodies
{
    public class SerializableObjectBody : IQuasiHttpBody
    {
        private IQuasiHttpBody _byteBufferBody;
        private Exception _srcEndError;

        public SerializableObjectBody(object content, Func<object, byte[]> serializationHandler, string contentType)
        {
            if (content == null)
            {
                throw new ArgumentException("null content");
            }
            if (serializationHandler == null)
            {
                throw new ArgumentException("null serialization handler");
            }
            Content = content;
            SerializationHandler = serializationHandler;
            ContentType = contentType ?? TransportUtils.ContentTypeJson;
        }

        public object Content { get; }

        public long ContentLength => -1;

        public string ContentType { get; }

        public Func<object, byte[]> SerializationHandler { get; }

        public async Task<int> ReadBytesAsync(IEventLoopApi eventLoop, byte[] data, int offset, int bytesToRead)
        {
            if (eventLoop == null)
            {
                throw new ArgumentException("null mutex api");
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
            if (_byteBufferBody == null)
            {
                try
                {
                    var srcData = SerializationHandler.Invoke(Content);
                    _byteBufferBody = new ByteBufferBody(srcData, 0, srcData.Length, null);
                }
                catch (Exception e)
                {
                    await EndReadInternally(eventLoop, e);
                    throw;
                }
            }
            return await _byteBufferBody.ReadBytesAsync(eventLoop, data, offset, bytesToRead);
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
            await EndReadInternally(eventLoop, e);
        }

        private async Task EndReadInternally(IEventLoopApi eventLoop, Exception e)
        {
            _srcEndError = e ?? new Exception("end of read");
            if (_byteBufferBody != null)
            {
                await _byteBufferBody.EndReadAsync(eventLoop, _srcEndError);
            }
        }
    }
}
