using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class TransportBackedBody : IQuasiHttpBody
    {
        private readonly CancellationTokenSource _readCancellationHandle = new CancellationTokenSource();
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly Func<Task> _closeCallback;
        private long _bytesRemaining;

        public TransportBackedBody(IQuasiHttpTransport transport, object connection,
             Func<Task> closeCallback, long contentLength, string contentType)
        {
            if (transport == null)
            {
                throw new ArgumentException("null transport");
            }
            _transport = transport;
            _connection = connection;
            _closeCallback = closeCallback;
            ContentType = contentType;
            ContentLength = contentLength;
            _bytesRemaining = -1;
            if (ContentLength >= 0)
            {
                _bytesRemaining = contentLength;
            }
        }

        public long ContentLength { get; private set; }

        public string ContentType { get; private set; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidMessagePayload(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (bytesToRead == 0 || _bytesRemaining == 0)
            {
                return 0;
            }
            if (_bytesRemaining > 0)
            {
                bytesToRead = (int)Math.Min(bytesToRead, _bytesRemaining);
            }
            int bytesRead = await _transport.ReadBytes(_connection, data, offset, bytesToRead);

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            if (_bytesRemaining > 0)
            {
                if (bytesRead == 0)
                {
                    var e = new Exception($"could not read remaining {_bytesRemaining} " +
                        $"bytes before end of read");
                    throw e;
                }
                _bytesRemaining -= bytesRead;
            }
            return bytesRead;
        }

        public async Task EndRead()
        {
            if (_readCancellationHandle.IsCancellationRequested)
            {
                return;
            }

            _readCancellationHandle.Cancel();
            if (_closeCallback != null)
            {
                await _closeCallback.Invoke();
            }
        }
    }
}
