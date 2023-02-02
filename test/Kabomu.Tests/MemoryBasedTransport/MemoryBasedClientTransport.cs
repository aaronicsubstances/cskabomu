using Kabomu.Common;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.MemoryBasedTransport
{
    /// <summary>
    /// Simulates the client-side of connection-oriented transports.
    /// </summary>
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        /// <summary>
        /// Creates a new instance of the <see cref="MemoryBasedClientTransport"/> class.
        /// </summary>
        public MemoryBasedClientTransport()
        {
        }

        /// <summary>
        /// Gets or sets the endpoint which should identify an instance of this class.
        /// </summary>
        public object LocalEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the virtual hub of servers connected to an instance of this class.
        /// Connection allocation is done through this dependency.
        /// </summary>
        public IMemoryBasedTransportHub Hub { get; set; }

        /// <summary>
        /// Allocates connections by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connectivityParams">server endpoint information as required by <see cref="Hub"/> dependency</param>
        /// <returns>a task whose result will contain a connection ready for use as a duplex
        /// stream of data for reading and writing</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.AllocateConnection(this, connectivityParams);
        }

        /// <summary>
        /// Releases a connection allocated by a instance of this class by
        /// forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connection">connection to release. null, invalid and already released connections are ignored.</param>
        /// <returns>task representing asynchronous operation</returns>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task ReleaseConnection(object connection)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.ReleaseClientConnection(this, connection);
        }

        /// <summary>
        /// Reads data from a connection returned from the <see cref="AllocateConnection(IConnectivityParams)"/>
        /// method by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connection">the connection to read from</param>
        /// <param name="data">the destination byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to read</param>
        /// <returns>a task representing the asynchronous read operation, whose result will
        /// be the number of bytes actually read. May be zero or less than the number of bytes requested.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="connection"/> or
        /// <paramref name="data"/> arguments is null</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/>
        /// arguments generate invalid offsets into <paramref name="data"/> argument.</exception>
        /// <exception cref="ArgumentException">The <paramref name="connection"/> argument is not a valid connection
        /// returned by instances of this class.</exception>
        /// <exception cref="ConnectionReleasedException">The connection has been released.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.ReadClientBytes(this, connection, data, offset, length);
        }

        /// <summary>
        /// Writes data to a connection returned from the <see cref="AllocateConnection(IConnectivityParams)"/>
        /// method by forwarding to the <see cref="Hub"/> dependency.
        /// </summary>
        /// <param name="connection">the connection to write to</param>
        /// <param name="data">the source byte buffer</param>
        /// <param name="offset">the starting position in data</param>
        /// <param name="length">the number of bytes to write</param>
        /// <returns>a task representing the asynchronous write operation</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="connection"/> or
        /// <paramref name="data"/> arguments is null</exception>
        /// <exception cref="ArgumentException">The <paramref name="offset"/> or <paramref name="length"/>
        /// arguments generate invalid offsets into <paramref name="data"/> argument.</exception>
        /// <exception cref="ArgumentException">The <paramref name="connection"/> argument is not a valid connection
        /// returned by instances of this class.</exception>
        /// <exception cref="ConnectionReleasedException">The connection has been released.</exception>
        /// <exception cref="MissingDependencyException">The <see cref="Hub"/> property is null.</exception>
        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var hub = Hub;
            if (hub == null)
            {
                throw new MissingDependencyException("transport hub");
            }
            return hub.WriteClientBytes(this, connection, data, offset, length);
        }
    }
}
