using Kabomu.Common;
using Kabomu.QuasiHttp.EntityBody;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.Tests.MemoryBasedTransport
{
    /// <summary>
    /// Simulates the server-side of connection-oriented transports.
    /// </summary>
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        private readonly object _mutex = new object();
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
        }

        /// <summary>
        /// Determines whether an instance of this class has been started.
        /// </summary>
        /// <returns>true if and only the instance of this class is running.</returns>
        public bool IsRunning()
        {
            lock (_mutex)
            {
                return _running;
            }
        }

        /// <summary>
        /// Starts the server so that it can respond to <see cref="ReceiveConnection"/> method calls
        /// in order to create connections. Calls to this method are ignored if an instance has already been started.
        /// </summary>
        /// <returns>task representing asynchronous operation.</returns>
        public Task Start()
        {
            lock (_mutex)
            {
                _running = true;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stops the instance from running, and fails all outstanding server and client
        /// connections with an instance of the <see cref="TransportResetException"/> type.
        /// Calls to this method are ignored if an instance has already been stopped.
        /// </summary>
        /// <returns>a task representing the asynchronous operation</returns>
        public Task Stop()
        {
            lock (_mutex)
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
            return Task.CompletedTask;
        }

        internal async Task<IConnectionAllocationResponse> CreateConnectionForClient(
            object serverEndpoint, object clientEndpoint)
        {
            Task<IConnectionAllocationResponse> connectTask;
            lock (_mutex)
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
                    ResolvePendingReceiveConnection();
                }
            }
            return await connectTask;
        }

        /// <summary>
        /// Returns the next connection received from clients, or waits for a client connection
        /// to arrive.
        /// </summary>
        /// <returns>task whose result will contain connection received from or allocated to a client</returns>
        /// <exception cref="TransportNotStartedException">if this instance is not running, ie has not been started.</exception>
        /// <exception cref="TransportResetException">If this instance is stopped while waiting for client connection</exception>
        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            Task<IConnectionAllocationResponse> connectTask;
            lock (_mutex)
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
                    ResolvePendingReceiveConnection();
                }
            }
            return await connectTask;
        }

        private void ResolvePendingReceiveConnection()
        {
            var pendingServerConnectRequest = _serverConnectRequests.First.Value;
            var pendingClientConnectRequest = _clientConnectRequests.First.Value;

            // do not invoke callbacks until state of this transport is updated,
            // to prevent error of re-entrant connection requests
            // matching previous ones.
            // (not really necessary for promise-based implementations).
            _serverConnectRequests.RemoveFirst();
            _clientConnectRequests.RemoveFirst();

            var connection = new MemoryBasedTransportConnectionInternal();
            var connectionAllocationResponseForServer = new DefaultConnectionAllocationResponse
            {
                Connection = connection
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

        public Task<bool> TrySerializeBody(object connection, byte[] prefix, IQuasiHttpBody body)
        {
            return Task.FromResult(false);
        }

        public Task<IQuasiHttpBody> DeserializeBody(object connection, long contentLength)
        {
            throw new NotImplementedException();
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