using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Transports
{
    public class MemoryBasedTransport : IQuasiHttpTransport
    {
        private readonly object _lock = new object();
        private readonly Random _randGen = new Random();
        private readonly LinkedList<ReceiveConnectionRequest> _onReceiveRequests = new LinkedList<ReceiveConnectionRequest>();
        private ReceiveConnectionRequest _receiveConnectionRequest;
        private bool _running;

        public MemoryBasedTransportHub Hub { get; set; }
        public double DirectSendRequestProcessingProbability { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public int MaxChunkSize { get; set; } = 8_192;

        public bool DirectSendRequestProcessingEnabled
        {
            get
            {
                lock (_lock)
                {
                    return _randGen.NextDouble() < DirectSendRequestProcessingProbability;
                }
            }
        }

        public Task Start()
        {
            lock (_lock)
            {
                _running = true;
            }
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            lock (_lock)
            {
                _running = false;
                var ex = new Exception("transport stopped");
                _receiveConnectionRequest?.Callback.SetException(ex);
                _receiveConnectionRequest = null;
                _onReceiveRequests.Clear();
            }
            return Task.CompletedTask;
        }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            if (connectionAllocationInfo?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            Task<IQuasiHttpResponse> responseTask;
            lock (_lock)
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                var remoteTransport = Hub.Transports[connectionAllocationInfo.RemoteEndpoint];
                responseTask = remoteTransport.Application.ProcessRequest(request, connectionAllocationInfo.Environment);
            }

            return responseTask;
        }

        public async Task<object> AllocateConnection(IConnectionAllocationRequest connectionAllocationRequest)
        {
            if (connectionAllocationRequest?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            lock (_lock)
            {
                var remoteTransport = Hub.Transports[connectionAllocationRequest.RemoteEndpoint];
                var connection = new MemoryBasedTransportConnectionInternal(this);                
                remoteTransport.OnReceive(connection);
                return connection;
            }
        }

        private void OnReceive(object connection)
        {
            lock (_lock)
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                var receiveRequest = new ReceiveConnectionRequest
                {
                    Connection = connection
                };
                _onReceiveRequests.AddLast(receiveRequest);
                if (_receiveConnectionRequest != null)
                {
                    ResolvePendingReceiveConnection();
                }
            }
        }

        public Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            lock (_lock)
            {
                if (!_running)
                {
                    throw new Exception("transport not started");
                }
                if (_receiveConnectionRequest != null)
                {
                    throw new Exception("pending receive connection yet to be resolved");
                }
                var receiveRequest = new ReceiveConnectionRequest
                {
                    Callback = new TaskCompletionSource<IConnectionAllocationResponse>(
                        TaskCreationOptions.RunContinuationsAsynchronously)
                };
                _receiveConnectionRequest = receiveRequest;
                if (_onReceiveRequests.Count > 0)
                {
                    ResolvePendingReceiveConnection();
                }
                return receiveRequest.Callback.Task;
            }
        }

        private void ResolvePendingReceiveConnection()
        {
            var pendingReceiveConnectionRequest = _receiveConnectionRequest;
            var pendingOnReceiveRequest = _onReceiveRequests.First.Value;
            var connectionAllocationResponse = new DefaultConnectionAllocationResponse
            {
                Connection = pendingOnReceiveRequest.Connection
            };

            // do not invoke callbacks until state of this transport is updated,
            // to prevent error of re-entrant receive connection requests
            // matching previous on-receives.
            _receiveConnectionRequest = null;
            _onReceiveRequests.RemoveFirst();

            pendingReceiveConnectionRequest.Callback.SetResult(connectionAllocationResponse);
        }

        public async Task ReleaseConnection(object connection, bool wasReceived)
        {
            Task releaseTask = null;
            lock (_lock)
            {
                if (connection is MemoryBasedTransportConnectionInternal typedConnection)
                {
                    releaseTask = typedConnection.Release();
                }
            }

            if (releaseTask != null)
            {
                await releaseTask;
            }
        }

        public async Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
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

            Task<int> readTask;
            lock (_lock)
            {
                readTask = typedConnection.ProcessReadRequest(this, data, offset, length);
            }

            int bytesRead = await readTask;
            return bytesRead;
        }

        public async Task WriteBytes(object connection, byte[] data, int offset, int length)
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

            Task writeTask;
            lock (_lock)
            {
                writeTask = typedConnection.ProcessWriteRequest(this, data, offset, length);
            }

            await writeTask;
        }

        class ReceiveConnectionRequest
        {
            public TaskCompletionSource<IConnectionAllocationResponse> Callback { get; set; }
            public object Connection { get; set; }
        }
    }
}
