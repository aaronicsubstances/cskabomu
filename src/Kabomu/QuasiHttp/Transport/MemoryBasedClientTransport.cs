using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        private readonly object _lock = new object();
        private readonly Random _randGen = new Random();

        public string LocalEndpoint { get; set; }
        public MemoryBasedTransportHub Hub { get; set; }
        public double DirectSendRequestProcessingProbability { get; set; }

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
                var remoteTransport = Hub.Servers[connectionAllocationInfo.RemoteEndpoint];
                if (!remoteTransport.IsRunning)
                {
                    throw new Exception("remote transport not started");
                }
                // can later pass local and remote endpoint information in request environment.
                responseTask = remoteTransport.Application.ProcessRequest(request, connectionAllocationInfo.Environment);
            }

            return responseTask;
        }

        public Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            if (connectionRequest?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            lock (_lock)
            {
                var remoteTransport = Hub.Servers[connectionRequest.RemoteEndpoint];
                var connection = new MemoryBasedTransportConnectionInternal(this, LocalEndpoint,
                    connectionRequest.RemoteEndpoint);
                remoteTransport.OnReceive(connection);
                return Task.FromResult<object>(connection);
            }
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(_lock, connection);
        }

        internal static async Task ReleaseConnectionInternal(object lockObj, object connection)
        {
            Task releaseTask = null;
            lock (lockObj)
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

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(this, _lock, connection, data, offset, length);
        }

        internal static async Task<int> ReadBytesInternal(IQuasiHttpTransport participant, object lockObj,
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

            Task<int> readTask;
            lock (lockObj)
            {
                readTask = typedConnection.ProcessReadRequest(participant, data, offset, length);
            }

            int bytesRead = await readTask;
            return bytesRead;
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(this, _lock, connection, data, offset, length);
        }

        internal static async Task WriteBytesInternal(IQuasiHttpTransport participant, object lockObj,
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

            Task writeTask;
            lock (lockObj)
            {
                writeTask = typedConnection.ProcessWriteRequest(participant, data, offset, length);
            }

            await writeTask;
        }
    }
}
