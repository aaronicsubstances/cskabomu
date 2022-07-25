using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class SerializableObjectBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private IQuasiHttpBody _backingBody;

        public SerializableObjectBody(object content, Func<object, byte[]> serializationHandler)
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
        }

        public Func<object, byte[]> SerializationHandler { get; }
        public object Content { get; }
        public long ContentLength => -1;
        public string ContentType { get; set; }

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
                _backingBody = new ByteBufferBody(srcData, 0, srcData.Length);
            }
            int bytesRead = await _backingBody.ReadBytes(data, offset, bytesToRead);
            return bytesRead;
        }

        public Task EndRead()
        {
            _readCancellationHandle.Cancel();
            // don't bother about ending read of backing body since it is just an in-memory object
            // and there is no contract to cancel ongoing reads.
            // that spares from dealing with possible null reference and memory inconsistency
            // in determining whether backing body has been initialized or not.
            return Task.CompletedTask;
        }
    }
}
