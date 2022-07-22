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

        public Task EndRead()
        {
            // To ensure thread safety, take advantage of the fact that if for some reason backing body was null here, 
            // that will be due to three reasons
            // 1. Read() has not been called; No possible problem since cancellation will be detected by any
            //       subsequent reads.
            // 2. Read() is in progress but the code creating a backing body has not been reached yet;
            //     No problem, there is no contract that an end read call should cancel an ongoing read.
            //     In any case that Read() call will be the last sucessful one.
            // 3. Read() is in progress and has created the backing body, but to due memory inconsistency error,
            //     the end read code here still sees the backing body as null.
            //     No problem just like in 2; that Read() call will be the last successful one.
            //
            // It also helps that in the absence of a protecting mutex,
            // backing body is not set to null here once it becomes non-null,
            // and that ensures read calls cannot experience any null pointer exceptions.
            _readCancellationHandle.Cancel();
            return _backingBody?.EndRead() ?? Task.CompletedTask;
            
        }
    }
}
