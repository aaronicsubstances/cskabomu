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
        private readonly Func<Task> _endOfReadCallback;
        private long _bytesRemaining;

        public TransportBackedBody(IQuasiHttpTransport transport, object connection,
             long contentLength, string contentType, Func<Task> endOfReadCallback)
        {
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }
            _transport = transport;
            _connection = connection;
            _endOfReadCallback = endOfReadCallback;
            ContentType = contentType;
            ContentLength = contentLength;
            if (ContentLength >= 0)
            {
                _bytesRemaining = contentLength;
            }
            else
            {
                _bytesRemaining = -1;
            }
        }

        public long ContentLength { get; }
        public string ContentType { get; }

        public async Task<int> ReadBytes(byte[] data, int offset, int bytesToRead)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, bytesToRead))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            // very important to return zero at this stage, because certain transport
            // implementations can choose to block forever after announcing the amount of data they are
            // returning, and returning all that data.
            // e.g. memory based implementation may just block forever if content length is nonnegative,
            // and bytes remaining is zero.
            if (_bytesRemaining == 0)
            {
                return 0;
            }

            if (_bytesRemaining >= 0)
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
            if (_endOfReadCallback != null)
            {
                await _endOfReadCallback.Invoke();
            }
        }
    }
}
