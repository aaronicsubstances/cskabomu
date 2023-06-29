using Kabomu.Common;
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
    public class TransportBackedBody : IQuasiHttpBody, IBytesAlreadyReadProviderInternal
    {
        private readonly ICancellationHandle _readCancellationHandle = new DefaultCancellationHandle();
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly bool _releaseConnection;

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
        }

        /// <summary>
        /// Returns the number of bytes to read from connection of a transport, or negative value
        /// to indicate unknown length, and hence all bytes of connection will be read.
        /// </summary>
        public long ContentLength { get; set; }

        public string ContentType { get; set; }

        long IBytesAlreadyReadProviderInternal.BytesAlreadyRead { get; set; }

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }

            EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

            async Task<int> ReadBytesInternal(int bytesToRead)
            {
                int bytesRead = await _transport.ReadBytes(_connection, data, offset, bytesToRead);

                EntityBodyUtilsInternal.ThrowIfReadCancelled(_readCancellationHandle);

                return bytesRead;
            }

            return EntityBodyUtilsInternal.PerformGeneralRead(this,
                length, ReadBytesInternal);
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
