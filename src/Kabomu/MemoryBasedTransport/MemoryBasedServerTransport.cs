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
        /// Creates a new instance.
        /// </summary>
        public MemoryBasedServerTransport()
        {
            _clientConnectRequests = new LinkedList<ClientConnectRequest>();
            _serverConnectRequests = new LinkedList<ServerConnectRequest>();
            MutexApi = new LockBasedMutexApi();
        }

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

        public async Task<bool> IsRunning()
        {
            using (await MutexApi.Synchronize())
            {
                return _running;
            }
        }

        public async Task Start()
        {
            using (await MutexApi.Synchronize())
            {
                _running = true;
            }
        }

        /// <summary>
        /// Stops the instance from running, and fails all outstanding server and client
        /// connections with a TransportResetException.
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
        /// Provide means to fulfil pending receive server connections by matching them with client connection requests.
        /// </summary>
        /// <param name="serverEndpoint">the endpoint used to identify this instance.</param>
        /// <param name="clientEndpoint">the endpoint the remote client identifies itself with</param>
        /// <returns>task whose result will contain connection allocated for a client</returns>
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

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(true, connection, data, offset, length);
        }

        internal static async Task<int> ReadBytesInternal(bool fromServer,
            object connection, byte[] data, int offset, int length)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            if (typedConnection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
            if (!ByteUtils.IsValidByteBufferSlice(data, offset, length))
            {
                throw new ArgumentException("invalid destination buffer");
            }
            int bytesRead = await typedConnection.ProcessReadRequest(fromServer, data, offset, length);
            return bytesRead;
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(true, connection, data, offset, length);
        }

        internal static async Task WriteBytesInternal(bool fromServer,
            object connection, byte[] data, int offset, int length)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            if (typedConnection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }
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