using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.EntityBody
{
    public class TransportCustomReader : ICustomReader
    {
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly bool _releaseConnection;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="transport">the quasi http transport to read from</param>
        /// <param name="connection">the connection to read from</param>
        /// <param name="releaseConnection">true if connection should be released during end of read; false
        /// if connection should not be released.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="transport"/> argument is null.</exception>
        public TransportCustomReader(IQuasiHttpTransport transport, object connection,
             bool releaseConnection)
        {
            if (transport == null)
            {
                throw new ArgumentNullException(nameof(transport));
            }
            _transport = transport;
            _connection = connection;
            _releaseConnection = releaseConnection;
        }

        public Task<int> ReadAsync(byte[] data, int offset, int length)
        {
            return _transport.ReadBytes(_connection, data, offset, length);
        }

        public Task CloseAsync()
        {
            if (_releaseConnection)
            {
                return _transport.ReleaseConnection(_connection);
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }
}
