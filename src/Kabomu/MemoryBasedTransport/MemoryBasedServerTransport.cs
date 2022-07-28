using Kabomu.Common;
using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.MemoryBasedTransport
{
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        private readonly LinkedList<ClientConnectRequest> _clientConnectRequests;
        private readonly LinkedList<ServerConnectRequest> _serverConnectRequests;
        private bool _running = false;

        public MemoryBasedServerTransport()
        {
            _clientConnectRequests = new LinkedList<ClientConnectRequest>();
            _serverConnectRequests = new LinkedList<ServerConnectRequest>();
            MutexApi = new LockBasedMutexApi();
        }

        public IMutexApi MutexApi { get; set; }
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

        public async Task Stop()
        {
            using (await MutexApi.Synchronize())
            {
                _running = false;
                var ex = new TransportStoppageException();
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

            // can later pass local and remote endpoint information in response environment.
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
                throw new ArgumentException("invalid payload");
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