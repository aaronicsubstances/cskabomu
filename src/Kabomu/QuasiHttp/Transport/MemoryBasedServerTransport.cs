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

        internal async Task<MemoryBasedTransportConnectionInternal> Connect(
            MemoryBasedClientTransport client,
            IConnectionAllocationRequest connectionRequest)
        {
            Task<MemoryBasedTransportConnectionInternal> connectTask;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                var connectRequest = new ClientConnectRequest
                {
                    Client = client,
                    ConnectionRequest = connectionRequest,
                    Callback = new TaskCompletionSource<MemoryBasedTransportConnectionInternal>(
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

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
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
                    throw new Exception("pending receive connection yet to be resolved");
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
                pendingClientConnectRequest.Client,
                pendingClientConnectRequest.ConnectionRequest.RemoteEndpoint);
            // can later add some environment variables.
            var connectionAllocationResponse = new DefaultConnectionAllocationResponse
            {
                Connection = connection
            };

            // do not invoke callbacks until state of this transport is updated,
            // to prevent error of re-entrant receive connection requests
            // matching previous on-receives.
            _serverConnectRequest = null;
            _clientConnectRequests.RemoveFirst();

            pendingServerConnectRequest.Callback.SetResult(connectionAllocationResponse);
            pendingClientConnectRequest.Callback.SetResult(connection);
        }

        public Task ReleaseConnection(object connection)
        {
            return MemoryBasedClientTransport.ReleaseConnectionInternal(MutexApi, connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedClientTransport.ReadBytesInternal(this, MutexApi, connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedClientTransport.WriteBytesInternal(this, MutexApi, connection, data, offset, length);
        }

        class ServerConnectRequest
        {
            public TaskCompletionSource<IConnectionAllocationResponse> Callback { get; set; }
        }

        private class ClientConnectRequest
        {
            public MemoryBasedClientTransport Client { get; set; }
            public IConnectionAllocationRequest ConnectionRequest { get; set; }
            public TaskCompletionSource<MemoryBasedTransportConnectionInternal> Callback { get; set; }
        }
    }
}