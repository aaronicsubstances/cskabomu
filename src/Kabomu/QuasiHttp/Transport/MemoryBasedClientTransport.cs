using Kabomu.Common;
using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        private readonly Random _randGen = new Random();

        public MemoryBasedClientTransport()
        {
            MutexApi = new LockBasedMutexApi(new object());
        }

        public string LocalEndpoint { get; set; }
        public MemoryBasedTransportHub Hub { get; set; }
        public double DirectSendRequestProcessingProbability { get; set; }
        public IMutexApi MutexApi { get; set; }

        public async Task<bool> CanProcessSendRequestDirectly()
        {
            using (await MutexApi.Synchronize())
            {
                return _randGen.NextDouble() < DirectSendRequestProcessingProbability;
            }
        }

        public async Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
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

            Task<MemoryBasedServerTransport> remoteTransportTask;
            using (await MutexApi.Synchronize())
            {
                remoteTransportTask = Hub.GetServer(connectionAllocationInfo.RemoteEndpoint);
            }

            var remoteTransport = await remoteTransportTask;
            // ensure remote transport is running, so as to have comparable behaviour
            // with allocate connection
            if (!await remoteTransport.IsRunning())
            {
                throw new Exception("remote transport not started");
            }
            var remoteApp = remoteTransport.Application;
            if (remoteApp == null)
            {
                throw new Exception("remote application not set");
            }
            // can later pass local and remote endpoint information in request environment.
            var response = await remoteApp.ProcessRequest(request, connectionAllocationInfo.Environment);
            return response;
        }

        public async Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            if (connectionRequest?.RemoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            Task<MemoryBasedServerTransport> remoteTransportTask;
            using (await MutexApi.Synchronize())
            {
                remoteTransportTask = Hub.GetServer(connectionRequest.RemoteEndpoint);
            }

            var remoteTransport = await remoteTransportTask;

            Task<MemoryBasedTransportConnectionInternal> connectionTask;
            using (await MutexApi.Synchronize())
            {
                connectionTask = remoteTransport.Connect(this, connectionRequest);
            }

            var connection = await connectionTask;
            return connection;
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(MutexApi, connection);
        }

        internal static async Task ReleaseConnectionInternal(IMutexApi mutexApi, object connection)
        {
            Task releaseTask = null;
            using (await mutexApi.Synchronize())
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
            return ReadBytesInternal(this, MutexApi, connection, data, offset, length);
        }

        internal static async Task<int> ReadBytesInternal(IQuasiHttpTransport participant, IMutexApi mutexApi,
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
            using (await mutexApi.Synchronize())
            {
                readTask = typedConnection.ProcessReadRequest(participant, data, offset, length);
            }

            int bytesRead = await readTask;
            return bytesRead;
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(this, MutexApi, connection, data, offset, length);
        }

        internal static async Task WriteBytesInternal(IQuasiHttpTransport participant, IMutexApi mutexApi,
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
            using (await mutexApi.Synchronize())
            {
                writeTask = typedConnection.ProcessWriteRequest(participant, data, offset, length);
            }

            await writeTask;
        }
    }
}
