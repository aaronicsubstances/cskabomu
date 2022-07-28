using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    /// <summary>
    /// Represents a stream of bytes from a connection of a quasi http transport.
    /// </summary>
    public class TransportBackedBody : IQuasiHttpBody
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly bool _releaseConnection;
        private long _bytesRemaining;

        public TransportBackedBody(IQuasiHttpTransport transport, object connection,
             long contentLength, bool releaseConnection)
        {
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }
            _transport = transport;
            _connection = connection;
            _releaseConnection = releaseConnection;
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
        public string ContentType { get; set; }

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
                    throw new ContentLengthNotSatisfiedException(ContentLength,
                        $"could not read remaining {_bytesRemaining} " +
                        $"bytes before end of read", null);
                }
                _bytesRemaining -= bytesRead;
            }
            return bytesRead;
        }

        public async Task EndRead()
        {
            if (!_readCancellationHandle.Cancel())
            {
                return;
            }
            if (_releaseConnection)
            {
                await _transport.ReleaseConnection(_connection);
            }
        }
    }
}
