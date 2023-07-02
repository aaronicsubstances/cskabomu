using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    /// <summary>
    /// Represents a stream of bytes from a connection of a quasi http transport.
    /// </summary>
    public class TransportCustomReaderWriter : ICustomReader, ICustomWriter
    {
        private readonly IQuasiHttpTransport _transport;
        private readonly object _connection;
        private readonly bool _releaseConnection;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="transport">the quasi http transport to use</param>
        /// <param name="connection">the connection to read from or write to</param>
        /// <param name="releaseConnection">true if connection should be released upon disposal; false
        /// if connection should be left open</param>
        /// <exception cref="ArgumentNullException">The <paramref name="transport"/> argument is null.</exception>
        public TransportCustomReaderWriter(IQuasiHttpTransport transport, object connection,
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

        public Task<int> ReadBytes(byte[] data, int offset, int length)
        {
            return _transport.ReadBytes(_connection, data, offset, length);
        }

        public Task WriteBytes(byte[] data, int offset, int length)
        {
            return _transport.WriteBytes(_connection, data, offset, length);
        }

        public Task CustomDispose()
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
