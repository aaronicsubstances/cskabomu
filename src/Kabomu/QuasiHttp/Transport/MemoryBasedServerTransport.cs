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
            MutexApi = new LockBasedMutexApi();
        }

        public IQuasiHttpApplication Application { get; set; }
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

        public async Task<object> CreateConnectionForClient(object serverEndpoint, object clientEndpoint,
            IMutexApi clientMutex)
        {
            Task<object> connectTask;
            Task resolveTask = null;
            using (await MutexApi.Synchronize())
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                var connectRequest = new ClientConnectRequest
                {
                    ServerEndpoint = serverEndpoint,
                    ClientEndpoint = clientEndpoint,
                    ClientMutex = clientMutex,
                    Callback = new TaskCompletionSource<object>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _clientConnectRequests.AddLast(connectRequest);
                connectTask = connectRequest.Callback.Task;
                if (_serverConnectRequest != null)
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

        public async Task<IQuasiHttpResponse> ProcessDirectSendRequest(object serverEndpoint, object clientEndpoint, 
            IMutexApi processingMutexApi, IQuasiHttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            IQuasiHttpApplication destApp;
            using (await MutexApi.Synchronize())
            {
                // ensure server is running, so as to have comparable behaviour
                // with receive connection
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                destApp = Application;
                if (destApp == null)
                {
                    throw new MissingDependencyException("server application");
                }
            }
            IMutexApi serverSideMutexApi = processingMutexApi;
            if (serverSideMutexApi == null && MutexApiFactory != null)
            {
                serverSideMutexApi = await MutexApiFactory.Create();
            }
            var environment = CreateInitialEnvironmentForQuasiHttpProcessingOptions(
                serverEndpoint, clientEndpoint);
            var processingOptions = new DefaultQuasiHttpProcessingOptions
            {
                RequestEnvironment = environment
            };
            var response = await destApp.ProcessRequest(request, processingOptions);
            return response;
        }

        private Dictionary<string, object> CreateInitialEnvironmentForQuasiHttpProcessingOptions(
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
            var pendingServerConnectRequest = _serverConnectRequest;
            var pendingClientConnectRequest = _clientConnectRequests.First.Value;

            // do not invoke callbacks until state of this transport is updated,
            // to prevent error of re-entrant connection requests
            // matching previous ones.
            // (not really necessary for promise-based implementations).
            _serverConnectRequest = null;
            _clientConnectRequests.RemoveFirst();

            IMutexApi serverSideMutexApi = null;
            if (MutexApiFactory != null)
            {
                serverSideMutexApi = await MutexApiFactory.Create();
            }
            var connection = new MemoryBasedTransportConnectionInternal(
                serverSideMutexApi, pendingClientConnectRequest.ClientMutex);
            var environment = CreateInitialEnvironmentForQuasiHttpProcessingOptions(
                pendingClientConnectRequest.ServerEndpoint,
                pendingClientConnectRequest.ClientEndpoint);
            var connectionAllocationResponse = new DefaultConnectionAllocationResponse
            {
                ProcessingMutexApi = serverSideMutexApi,
                Connection = connection,
                Environment = environment
            };

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
                throw new ArgumentException("null connection");
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

        private class ClientConnectRequest
        {
            public object ServerEndpoint { get; set; }
            public object ClientEndpoint { get; set; }
            public IMutexApi ClientMutex { get; set; }
            public TaskCompletionSource<object> Callback { get; set; }
        }
    }
}