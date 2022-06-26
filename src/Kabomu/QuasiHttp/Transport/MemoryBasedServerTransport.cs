using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedServerTransport : IQuasiHttpServerTransport
    {
        private readonly object _lock = new object();
        private bool _running = false;
        private readonly LinkedList<ReceiveConnectionRequest> _onReceiveRequests = new LinkedList<ReceiveConnectionRequest>();
        private ReceiveConnectionRequest _receiveConnectionRequest;

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _running;
                }
            }
        }

        public IQuasiHttpApplication Application { get; set; }

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

        internal void OnReceive(MemoryBasedTransportConnectionInternal connection)
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

        public Task ReleaseConnection(object connection)
        {
            return MemoryBasedClientTransport.ReleaseConnectionInternal(_lock, connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedClientTransport.ReadBytesInternal(this, _lock, connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedClientTransport.WriteBytesInternal(this, _lock, connection, data, offset, length);
        }

        class ReceiveConnectionRequest
        {
            public TaskCompletionSource<IConnectionAllocationResponse> Callback { get; set; }
            public MemoryBasedTransportConnectionInternal Connection { get; set; }
        }
    }
}