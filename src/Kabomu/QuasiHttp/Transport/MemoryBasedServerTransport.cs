using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        private readonly LinkedList<ClientConnectRequest> _clientConnectRequests;
        private ServerConnectRequest _serverConnectRequest;
        private bool _running = false;

        public MemoryBasedServerTransport()
        {
            _clientConnectRequests = new LinkedList<ClientConnectRequest>();
            MutexApi = new LockBasedMutexApi(new object());
        }

        public IMutexApi MutexApi { get; set; }
        public IQuasiHttpApplication Application { get; set; }

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
                var ex = new Exception("transport stopped");
                _serverConnectRequest?.Callback.SetException(ex);
                foreach (var clientConnectRequest in _clientConnectRequests)
                {
                    clientConnectRequest.Callback.SetException(ex);
                }
                _serverConnectRequest = null;
                _clientConnectRequests.Clear();
            }
        }

        public async Task<object> CreateConnectionForClient(IConnectionAllocationRequest connectionRequest,
            string clientEndpoint, IMutexApi clientMutex)
        {
            if (connectionRequest?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null server endpoint");
            }
            if (clientEndpoint == null)
            {
                throw new ArgumentException("null client endpoint");
            }
            if (clientMutex == null)
            {
                throw new ArgumentException("null client mutex");
            }
            Task<object> connectTask;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                var connectRequest = new ClientConnectRequest
                {
                    ConnectionRequest = connectionRequest,
                    ClientEndpoint = clientEndpoint,
                    ClientMutex = clientMutex,
                    Callback = new TaskCompletionSource<object>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _clientConnectRequests.AddLast(connectRequest);
                connectTask = connectRequest.Callback.Task;
                if (_serverConnectRequest != null)
                {
                    ResolvePendingReceiveConnection();
                }
            }
            return await connectTask;
        }

        public Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            return CreateConnectionForServer();
        }

        private async Task<IConnectionAllocationResponse> CreateConnectionForServer()
        {
            Task<IConnectionAllocationResponse> connectTask;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                if (_serverConnectRequest != null)
                {
                    throw new Exception("pending server connect request yet to be resolved");
                }
                _serverConnectRequest = new ServerConnectRequest
                {
                    Callback = new TaskCompletionSource<IConnectionAllocationResponse>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                connectTask = _serverConnectRequest.Callback.Task;
                if (_clientConnectRequests.Count > 0)
                {
                    ResolvePendingReceiveConnection();
                }
            }
            return await connectTask;
        }

        private void ResolvePendingReceiveConnection()
        {
            var pendingServerConnectRequest = _serverConnectRequest;
            var pendingClientConnectRequest = _clientConnectRequests.First.Value;

            var connection = new MemoryBasedTransportConnectionInternal(
                MutexApi, pendingClientConnectRequest.ClientMutex);
            var connectionAllocationResponse = new DefaultConnectionAllocationResponse
            {
                Connection = connection
            };

            // do not invoke callbacks until state of this transport is updated,
            // to prevent error of re-entrant connection requests
            // matching previous ones.
            // (not really necessary for promise-based implementations).
            _serverConnectRequest = null;
            _clientConnectRequests.RemoveFirst();

            // can later pass local and remote endpoint information in response environment.
            pendingServerConnectRequest.Callback.SetResult(connectionAllocationResponse);
            pendingClientConnectRequest.Callback.SetResult(connection);
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
                throw new ArgumentException("null connection");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
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
                throw new ArgumentException("null connection");
            }
            if (!ByteUtils.IsValidMessagePayload(data, offset, length))
            {
                throw new ArgumentException("invalid payload");
            }
            await typedConnection.ProcessWriteRequest(fromServer, data, offset, length);
        }

        class ServerConnectRequest
        {
            public TaskCompletionSource<IConnectionAllocationResponse> Callback { get; set; }
        }

        private class ClientConnectRequest
        {
            public IConnectionAllocationRequest ConnectionRequest { get; set; }
            public object ClientEndpoint { get; set; }
            public IMutexApi ClientMutex { get; set; }
            public TaskCompletionSource<object> Callback { get; set; }
        }
    }
}