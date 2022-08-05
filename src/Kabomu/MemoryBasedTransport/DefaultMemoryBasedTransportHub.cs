using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    /// <summary>
    /// Implements the <see cref="IMemoryBasedTransportHub"/> interface based on dictionary of <see cref="IQuasiHttpServer"/>
    /// instances. Also requires instances of <see cref="MemoryBasedServerTransport"/> for use in
    /// allocating connections.
    /// </summary>
    public class DefaultMemoryBasedTransportHub : IMemoryBasedTransportHub
    {
        private readonly Dictionary<object, IQuasiHttpServer> _servers;

        /// <summary>
        /// Creates a new instance of the <see cref="DefaultMemoryBasedTransportHub"/> class
        /// with an internally allocated dictionary.
        /// </summary>
        /// <remarks>
        /// Only dictionary addition operations can be performed with this constructor.
        /// </remarks>
        public DefaultMemoryBasedTransportHub()
            : this(new Dictionary<object, IQuasiHttpServer>())
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
        public DefaultMemoryBasedTransportHub(Dictionary<object, IQuasiHttpServer> servers)
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
        /// Associates a quasi http server with an endpoint.
        /// </summary>
        /// <param name="endpoint">the endpoint associated with this server.</param>
        /// <param name="server">the server associated with the endpoint</param>
        /// <returns>task representing the asynchronous add operation</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="endpoint"/> argument or
        /// <paramref name="server"/> argument is null</exception>
        /// <exception cref="ArgumentException">The <paramref name="endpoint"/> argument is already in
        /// use with another server.</exception>
        public async Task AddServer(object endpoint, IQuasiHttpServer server)
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
                _servers.Add(endpoint, server);
            }
        }

        /// <summary>
        /// Locates a server added with a given endpoint, and then tries to forward a request to its
        /// application property.
        /// </summary>
        /// <param name="client">the client making the send request</param>
        /// <param name="connectivityParams">source of extraction of remote endpoint value which will be used to
        /// search for an attached server</param>
        /// <param name="request">the request to process</param>
        /// <returns>a task whose result is the response from the attached server to the request argument</returns>
        /// <exception cref="ArgumentException">The <paramref name="connectivityParams"/> argument is null or contains a
        /// null remote endpoint</exception>
        /// <exception cref="ArgumentNullException">The <paramref name="request"/> argument is null</exception>
        /// <exception cref="MissingDependencyException">No attached server was found for the extracted remote endpoint.</exception>
        /// <exception cref="MissingDependencyException">No application was found on the attached server associated with the
        /// extracted remote endpoint.</exception>
        public async Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpClientTransport client,
            IConnectivityParams connectivityParams, IQuasiHttpRequest request)
        {
            var serverEndpoint = connectivityParams?.RemoteEndpoint;
            if (serverEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IQuasiHttpApplication destApp;
            using (await MutexApi.Synchronize())
            {
                IQuasiHttpServer server = null;
                if (_servers.ContainsKey(serverEndpoint))
                {
                    server = _servers[serverEndpoint];
                }
                if (server == null)
                {
                    throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
                }
                destApp = server.Application;
            }

            if (destApp == null)
            {
                throw new MissingDependencyException("remote server application");
            }
            object clientEndpoint = null;
            if (client is MemoryBasedClientTransport)
            {
                clientEndpoint = ((MemoryBasedClientTransport)client).LocalEndpoint;
            }
            var environment = MemoryBasedServerTransport.CreateRequestEnvironment(
                serverEndpoint, clientEndpoint);
            var response = await destApp.ProcessRequest(request, environment);
            return response;
        }

        /// <summary>
        /// Locates an attached server, and then tries to allocate a connection directly on its transport property, which
        /// must be an instance of <see cref="MemoryBasedServerTransport"/>.
        /// </summary>
        /// <param name="client">the client allocating the connection</param>
        /// <param name="connectivityParams">source of extraction of remote endpoint value which will be used to
        /// search for an attached server</param>
        /// <returns>a task whose result willc contain an allocated connection from attached server for extracted remote
        /// endpoint to the requesting client</returns>
        /// <exception cref="ArgumentException">The <paramref name="connectivityParams"/> is null or contains null remote
        /// endpoint</exception>
        /// <exception cref="MissingDependencyException">No attached server was found for the extracted remote endpoint.</exception>
        /// <exception cref="MissingDependencyException">No transport was found on the attached server associated with the
        /// extracted remote endpoint; or the transport found is not an instance of <see cref="MemoryBasedServerTransport"/></exception>
        public async Task<IConnectionAllocationResponse> AllocateConnection(IQuasiHttpClientTransport client,
            IConnectivityParams connectivityParams)
        {
            var serverEndpoint = connectivityParams?.RemoteEndpoint;
            if (serverEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }

            IQuasiHttpServerTransport serverTransport;
            using (await MutexApi.Synchronize())
            {
                IQuasiHttpServer server = null;
                if (_servers.ContainsKey(serverEndpoint))
                {
                    server = _servers[serverEndpoint];
                }
                if (server == null)
                {
                    throw new MissingDependencyException("missing server for given endpoint: " + serverEndpoint);
                }
                serverTransport = server.Transport;
            }

            if (serverTransport == null)
            {
                throw new MissingDependencyException("remote server transport");
            }

            if (serverTransport is MemoryBasedServerTransport memoryBasedServerTransport)
            {
                object clientEndpoint = null;
                var memoryBasedClientTransport = client as MemoryBasedClientTransport;
                if (memoryBasedClientTransport != null)
                {
                    clientEndpoint = memoryBasedClientTransport.LocalEndpoint;
                }
                var connectionAllocationResponse = await memoryBasedServerTransport.CreateConnectionForClient(
                    serverEndpoint, clientEndpoint);
                if (memoryBasedClientTransport != null)
                {
                    var memoryBasedConnection = (MemoryBasedTransportConnectionInternal)connectionAllocationResponse.Connection;
                    memoryBasedConnection.SetMaxWriteBufferLimit(false, memoryBasedClientTransport.MaxWriteBufferLimit);
                }
                return connectionAllocationResponse;
            }
            else
            {
                throw new MissingDependencyException("remote server transport is not memory based");
            }
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
