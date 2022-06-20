using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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

            Task<int> readTask;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    throw _srcEndError;
                }
                if (_byteBufferBody == null)
                {
                    var srcData = SerializationHandler.Invoke(Content);
                    _byteBufferBody = new ByteBufferBody(srcData, 0, srcData.Length, null);
                }
                readTask = _byteBufferBody.ReadBytes(eventLoop, data, offset, bytesToRead);
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

            Task endTask = null;
            lock (eventLoop)
            {
                if (_srcEndError != null)
                {
                    return;
                }
                _srcEndError = e ?? new Exception("end of read");
                if (_byteBufferBody != null)
                {
                    endTask = _byteBufferBody.EndRead(eventLoop, _srcEndError);
                }
            }

            if (endTask != null)
            {
                await endTask;
            }
        }
    }
}
