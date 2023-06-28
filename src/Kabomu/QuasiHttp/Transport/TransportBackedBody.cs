using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
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

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="transport">the quasi http transport to read from</param>
        /// <param name="connection">the connection to read from</param>
        /// <param name="contentLength">the number of bytes to read; or -1 or any negative value
        /// to read all bytes in the connection.</param>
        /// <param name="releaseConnection">true if connection should be released during end of read; false
        /// if connection should not be released.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="transport"/> argument is null.</exception>
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

        /// <summary>
        /// Returns the number of bytes to read from connection of a transport, or negative value
        /// to indicate unknown length, and hence all bytes of connection will be read.
        /// </summary>
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
