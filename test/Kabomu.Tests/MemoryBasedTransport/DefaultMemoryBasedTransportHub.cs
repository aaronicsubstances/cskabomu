using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.MemoryBasedTransport
{
    /// <summary>
    /// Connects instances of <see cref="MemoryBasedClientTransport"/> to that of
    /// <see cref="MemoryBasedServerTransport"/>.
    /// </summary>
    public class DefaultMemoryBasedTransportHub : IMemoryBasedTransportHub
    {
        private readonly Dictionary<object, MemoryBasedServerTransport> _servers;

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultMemoryBasedTransportHub"/> class
        /// with an internally allocated dictionary.
        /// </summary>
        /// <remarks>
        /// Only dictionary addition operations can be performed with this constructor.
        /// </remarks>
        public DefaultMemoryBasedTransportHub()
            : this(new Dictionary<object, MemoryBasedServerTransport>())
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultMemoryBasedTransportHub"/> class
        /// with an externally allocated dictionary.
        /// </summary>
        /// <remarks>
        /// This constructor provides flexibility in having full access to the collection of
        /// servers.
        /// </remarks>
        /// <param name="servers">the collection instance of servers to use</param>
        /// <exception cref="ArgumentNullException">The <paramref name="servers"/> argument is null.</exception>
        public DefaultMemoryBasedTransportHub(Dictionary<object, MemoryBasedServerTransport> servers)
        {
            if (servers == null)
            {
                throw new ArgumentNullException(nameof(servers));
            }
            _servers = servers;
            MutexApi = new LockBasedMutexApi();
        }

        /// <summary>
        /// Gets or sets mutex api used to guard multithreaded 
        /// access to operations of this class.
        /// </summary>
        /// <remarks> 
        /// An ordinary lock object is the initial value for this property, and so there is no need to modify
        /// this property except for advanced scenarios.
        /// </remarks>
        public IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Associates a quasi http server transport with an endpoint. Must be of the type
        /// <see cref="MemoryBasedServerTransport"/>.
        /// </summary>
        /// <param name="endpoint">the endpoint associated with this server.</param>
        /// <param name="server">the server associated with the endpoint</param>
        /// <returns>task representing the asynchronous add operation</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="endpoint"/> argument or
        /// <paramref name="server"/> argument is null</exception>
        /// <exception cref="ArgumentException">The <paramref name="endpoint"/> argument is already in
        /// use with another server.</exception>
        public async Task AddServer(object endpoint, IQuasiHttpServerTransport server)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            if (server == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }
            using (await MutexApi.Synchronize())
            {
                if (_servers.ContainsKey(endpoint))
                {
                    throw new ArgumentException("an association already exists for endpoint: " + endpoint);
                }
                if (server is MemoryBasedServerTransport t)
                {
                    _servers.Add(endpoint, t);
                }
                else
                {
                    throw new ArgumentException("server must be an instance of MemoryBasedServerTransport");
                }
            }
        }

        /// <summary>
        /// Locates an attached server, and then uses it to allocate a connection.
        /// </summary>
        /// <param name="client">the client allocating the connection</param>
        /// <param name="connectivityParams">source of extraction of remote endpoint value which will be used to
        /// search for an attached server</param>
        /// <returns>a task whose result willc contain an allocated connection from attached server for extracted remote
        /// endpoint to the requesting client</returns>
        /// <exception cref="ArgumentException">The <paramref name="connectivityParams"/> is null or contains null remote
        /// endpoint</exception>
        /// <exception cref="MissingDependencyException">No attached server was found for the extracted remote endpoint.</exception>
        public async Task<IConnectionAllocationResponse> AllocateConnection(IQuasiHttpClientTransport client,
            IConnectivityParams connectivityParams)
        {
            var serverEndpoint = connectivityParams?.RemoteEndpoint;
            if (serverEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }

            MemoryBasedServerTransport server = null;
            using (await MutexApi.Synchronize())
            {
                if (_servers.ContainsKey(serverEndpoint))
                {
                    server = _servers[serverEndpoint];
                }
            }

            if (server == null)
            {
                throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
            }

            object clientEndpoint = null;
            var memoryBasedClientTransport = client as MemoryBasedClientTransport;
            if (memoryBasedClientTransport != null)
            {
                clientEndpoint = memoryBasedClientTransport.LocalEndpoint;
            }
            var connectionAllocationResponse = await server.CreateConnectionForClient(
                serverEndpoint, clientEndpoint);
            return connectionAllocationResponse;
        }

        /// <summary>
        /// Reads bytes from connections created by instances of the <see cref="MemoryBasedServerTransport"/> class.
        /// </summary>
        /// <param name="client">the client making the read request</param>
        /// <param name="connection">the connection to read from. Must be a connection created by
        /// <see cref="MemoryBasedServerTransport"/> class</param>
        /// <param name="data">destination byte buffer of read request</param>
        /// <param name="offset">starting position in data buffer</param>
        /// <param name="length">number of bytes to read</param>
        /// <returns>a task whose result will be the number of bytes actually read. that number may be
        /// less than that requested.</returns>
        public Task<int> ReadClientBytes(IQuasiHttpClientTransport client, object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.ReadBytesInternal(false, connection, data, offset, length);
        }

        /// <summary>
        /// Write bytes to connections created by instances of the <see cref="MemoryBasedServerTransport"/> class.
        /// </summary>
        /// <param name="client">the client making the read request</param>
        /// <param name="connection">the connection to write to. Must be a connection created by
        /// <see cref="MemoryBasedServerTransport"/> class</param>
        /// <param name="data">source byte buffer of write request</param>
        /// <param name="offset">starting position in data buffer</param>
        /// <param name="length">number of bytes to write</param>
        /// <returns>a task representing the asynchronous operation</returns>
        public Task WriteClientBytes(IQuasiHttpClientTransport client, object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.WriteBytesInternal(false, connection, data, offset, length);
        }

        /// <summary>
        /// Releases connections created by instances of the <see cref="MemoryBasedServerTransport"/> class.
        /// </summary>
        /// <param name="client">the client requesting for a connection to be released. Must be a connection created by
        /// <see cref="MemoryBasedServerTransport"/> class to take effect.</param>
        /// <param name="connection">the connection to be released</param>
        /// <returns>a task representing the asynchronous operation</returns>
        public Task ReleaseClientConnection(IQuasiHttpClientTransport client, object connection)
        {
            return MemoryBasedServerTransport.ReleaseConnectionInternal(connection);
        }
    }
}
