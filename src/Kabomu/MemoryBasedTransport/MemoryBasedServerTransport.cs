using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    /// <summary>
    /// Implements the standard in-memory connection-oriented server-side quasi http transport provided by the
    /// Kabomu library.
    /// </summary>
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        private readonly LinkedList<ClientConnectRequest> _clientConnectRequests;
        private readonly LinkedList<ServerConnectRequest> _serverConnectRequests;
        private bool _running = false;

        /// <summary>
        /// Creates a new instance of the <see cref="MemoryBasedServerTransport"/> class.
        /// </summary>
        public MemoryBasedServerTransport()
        {
            _clientConnectRequests = new LinkedList<ClientConnectRequest>();
            _serverConnectRequests = new LinkedList<ServerConnectRequest>();
            MutexApi = new LockBasedMutexApi();
        }

        /// <summary>
        /// Gets or sets the maximum write buffer limit for connections which will be created by
        /// this class. A positive value means that
        /// any attempt to write (excluding last writes) such that the total number of
        /// bytse outstanding tries to exceed that positive value, will result in an instance of the
        /// <see cref="DataBufferLimitExceededException"/> class to be thrown.
        /// <para></para>
        /// By default this property is zero, and so indicates that the default value of 65,6536 bytes
        /// will be used as the maximum write buffer limit.
        /// </summary>
        public int MaxWriteBufferLimit { get; set; }

        /// <summary>
        /// Gets or sets mutex api used to guard multithreaded 
        /// access to connection creation operations of this class.
        /// </summary>
        /// <remarks> 
        /// An ordinary lock object is the initial value for this property, and so there is no need to modify
        /// this property except for advanced scenarios.
        /// </remarks>
        public IMutexApi MutexApi { get; set; }

        /// <summary>
        /// Gets or sets factory for supplying alternative to ordinary lock objects used to guard multithreaded access
        /// to connection usage opertions of this class
        /// </summary>
        /// <remarks> 
        /// A factory supplying ordinary lock objects is the initial value for this property, and so there is no need to modify
        /// this property except for advanced scenarios.
        /// </remarks>
        public IMutexApiFactory MutexApiFactory { get; set; }

        /// <summary>
        /// Determines whether an instance of this class has been started.
        /// </summary>
        /// <returns>task whose result is true if and only is an instance of this class is running.</returns>
        public async Task<bool> IsRunning()
        {
            using (await MutexApi.Synchronize())
            {
                return _running;
            }
        }

        /// <summary>
        /// Starts the server so that it can respond to <see cref="ReceiveConnection"/> method calls
        /// in order to create connections. Calls to this method are ignored if an instance has already been started.
        /// </summary>
        /// <returns>task representing asynchronous operation.</returns>
        public async Task Start()
        {
            using (await MutexApi.Synchronize())
            {
                _running = true;
            }
        }

        /// <summary>
        /// Stops the instance from running, and fails all outstanding server and client
        /// connections with an instance of the <see cref="TransportResetException"/> type.
        /// Calls to this method are ignored if an instance has already been stopped.
        /// </summary>
        /// <returns>a task representing the asynchronous operation</returns>
        public async Task Stop()
        {
            using (await MutexApi.Synchronize())
            {
                _running = false;
                var ex = new TransportResetException();
                foreach (var serverConnectRequest in _serverConnectRequests)
                {
                    serverConnectRequest.Callback.SetException(ex);
                }
                foreach (var clientConnectRequest in _clientConnectRequests)
                {
                    clientConnectRequest.Callback.SetException(ex);
                }
                _serverConnectRequests.Clear();
                _clientConnectRequests.Clear();
            }
        }

        /// <summary>
        /// Fulfils pending receive server connections, ie calls to the <see cref="ReceiveConnection"/> method,
        /// by matching them with client connection requests.
        /// </summary>
        /// <param name="serverEndpoint">cthe endpoint used to identify this instance</param>
        /// <param name="clientEndpoint">the endpoint the remote client identifies itself with</param>
        /// <returns>task whose result will contain connection allocated for a client</returns>
        /// <exception cref="TransportNotStartedException">if this instance is not running, ie has not been started.</exception>
        /// <exception cref="TransportResetException">If this instance is stopped while waiting for server connection</exception>
        public async Task<IConnectionAllocationResponse> CreateConnectionForClient(object serverEndpoint, object clientEndpoint)
        {
            Task<IConnectionAllocationResponse> connectTask;
            Task resolveTask = null;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    throw new TransportNotStartedException();
                }
                var connectRequest = new ClientConnectRequest
                {
                    ServerEndpoint = serverEndpoint,
                    ClientEndpoint = clientEndpoint,
                    Callback = new TaskCompletionSource<IConnectionAllocationResponse>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _clientConnectRequests.AddLast(connectRequest);
                connectTask = connectRequest.Callback.Task;
                if (_serverConnectRequests.Count > 0)
                {
                    resolveTask = ResolvePendingReceiveConnection();
                }
            }
            if (resolveTask != null)
            {
                await resolveTask;
            }
            return await connectTask;
        }

        internal static Dictionary<string, object> CreateRequestEnvironment(
            object serverEndpoint, object clientEndpoint)
        {
            // can later pass in server and client endpoint information.
            var environment = new Dictionary<string, object>();
            return environment;
        }

        /// <summary>
        /// Returns the next connection received from clients, or waits for a client connection
        /// to arrive, ie via the <see cref="CreateConnectionForClient(object, object)"/> method.
        /// </summary>
        /// <returns>task whose result will contain connection received from or allocated to a client</returns>
        /// <exception cref="TransportNotStartedException">if this instance is not running, ie has not been started.</exception>
        /// <exception cref="TransportResetException">If this instance is stopped while waiting for client connection</exception>
        public Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            return CreateConnectionForServer();
        }

        private async Task<IConnectionAllocationResponse> CreateConnectionForServer()
        {
            Task<IConnectionAllocationResponse> connectTask;
            Task resolveTask = null;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    throw new TransportNotStartedException();
                }
                var serverConnectRequest = new ServerConnectRequest
                {
                    Callback = new TaskCompletionSource<IConnectionAllocationResponse>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _serverConnectRequests.AddLast(serverConnectRequest);
                connectTask = serverConnectRequest.Callback.Task;
                if (_clientConnectRequests.Count > 0)
                {
                    resolveTask = ResolvePendingReceiveConnection();
                }
            }
            if (resolveTask != null)
            {
                await resolveTask;
            }
            return await connectTask;
        }

        private async Task ResolvePendingReceiveConnection()
        {
            var pendingServerConnectRequest = _serverConnectRequests.First.Value;
            var pendingClientConnectRequest = _clientConnectRequests.First.Value;

            // do not invoke callbacks until state of this transport is updated,
            // to prevent error of re-entrant connection requests
            // matching previous ones.
            // (not really necessary for promise-based implementations).
            _serverConnectRequests.RemoveFirst();
            _clientConnectRequests.RemoveFirst();

            IMutexApi serverSideMutexApi = null, clientSideMutexApi = null;
            if (MutexApiFactory != null)
            {
                serverSideMutexApi = await MutexApiFactory.Create();
                clientSideMutexApi = await MutexApiFactory.Create();
            }
            var connection = new MemoryBasedTransportConnectionInternal(
                serverSideMutexApi, clientSideMutexApi);
            connection.SetMaxWriteBufferLimit(true, MaxWriteBufferLimit);
            var requestEnvironment = CreateRequestEnvironment(
                pendingClientConnectRequest.ServerEndpoint,
                pendingClientConnectRequest.ClientEndpoint);
            var connectionAllocationResponseForServer = new DefaultConnectionAllocationResponse
            {
                Connection = connection,
                Environment = requestEnvironment
            };
            var connectionAllocationResponseForClient = new DefaultConnectionAllocationResponse
            {
                Connection = connection
            };

            pendingServerConnectRequest.Callback.SetResult(connectionAllocationResponseForServer);
            pendingClientConnectRequest.Callback.SetResult(connectionAllocationResponseForClient);
        }

        /// <summary>
        /// Releases a connection created by a instance of this class.
        /// </summary>
        /// <param name="connection">connection to release. null, invalid and already released connections are ignored.</param>
        /// <returns>task representing asynchronous operation</returns>
        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static async Task ReleaseConnectionInternal(object connection)
        {
            if (connection is MemoryBasedTransportConnectionInternal typedConnection)
            {
                await typedConnection.Release();
            }
        }

        /// <summary>
        /// Reads data from a connection returned from the <see cref="ReceiveConnection"/>
        /// method.
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
        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(true, connection, data, offset, length);
        }

        internal static async Task<int> ReadBytesInternal(bool fromServer,
            object connection, byte[] data, int offset, int length)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (!(connection is MemoryBasedTransportConnectionInternal))
            {
                throw new ArgumentException("invalid connection", nameof(connection));
            }
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }
            int bytesRead = await typedConnection.ProcessReadRequest(fromServer, data, offset, length);
            return bytesRead;
        }

        /// <summary>
        /// Writes data to a connection returned from the <see cref="ReceiveConnection"/>
        /// method.
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
        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(true, connection, data, offset, length);
        }

        internal static async Task WriteBytesInternal(bool fromServer,
            object connection, byte[] data, int offset, int length)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (!(connection is MemoryBasedTransportConnectionInternal))
            {
                throw new ArgumentException("invalid connection", nameof(connection));
            }
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid payload");
            }
            await typedConnection.ProcessWriteRequest(fromServer, data, offset, length);
        }

        class ServerConnectRequest
        {
            public TaskCompletionSource<IConnectionAllocationResponse> Callback { get; set; }
        }

        class ClientConnectRequest
        {
            public object ServerEndpoint { get; set; }
            public object ClientEndpoint { get; set; }
            public TaskCompletionSource<IConnectionAllocationResponse> Callback { get; set; }
        }
    }
}