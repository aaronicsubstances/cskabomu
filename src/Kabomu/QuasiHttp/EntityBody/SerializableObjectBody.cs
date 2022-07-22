using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class SerializableObjectBody : IQuasiHttpBody
    {
        private readonly CancellationTokenSource _readCancellationHandle = new CancellationTokenSource();
        private IQuasiHttpBody _backingBody;

        public SerializableObjectBody(object content, Func<object, byte[]> serializationHandler, string contentType)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }
            if (serializationHandler == null)
            {
                throw new ArgumentNullException(nameof(serializationHandler));
            }
            Content = content;
            SerializationHandler = serializationHandler;
            ContentType = contentType;
        }

        public Func<object, byte[]> SerializationHandler { get; }
        public object Content { get; }
        public long ContentLength => -1;
        public string ContentType { get; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (_backingBody == null)
            {
                var srcData = SerializationHandler.Invoke(Content);
                _backingBody = new ByteBufferBody(srcData, 0, srcData.Length, null);
            }
            int bytesRead = await _backingBody.ReadBytes(data, offset, bytesToRead);
            return bytesRead;
        }

        public async Task EndRead()
        {
            _readCancellationHandle.Cancel();
            // take advantage of the fact once backing body is not null,
            // no code in this class sets it back to null.
            if (_backingBody != null)
            {
                await _backingBody.EndRead();
            }
        }
    }
}
