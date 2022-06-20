using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common.Transports
{
    public class MemoryBasedTransport : IQuasiHttpTransport
    {
        private readonly Random _randGen = new Random();

        public MemoryBasedTransportHub Hub { get; set; }
        public double DirectSendRequestProcessingProbability { get; set; }

        public int MaxChunkSize { get; set; } = 8_192;

        public bool DirectSendRequestProcessingEnabled => _randGen.NextDouble() < DirectSendRequestProcessingProbability;

        public IEventLoopApi EventLoop { get; set; }
        public UncaughtErrorCallback ErrorHandler { get; set; }

        public Task<IQuasiHttpResponse> ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            Task<IQuasiHttpResponse> responseTask;
            lock (EventLoop)
            {
                var remoteClient = Hub.Clients[remoteEndpoint];
                responseTask = remoteClient.Application.ProcessRequest(request);
            }

            return responseTask;
        }

        public async Task<object> AllocateConnection(object remoteEndpoint)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            lock (EventLoop)
            {
                var remoteClient = Hub.Clients[remoteEndpoint];
                var connection = new MemoryBasedTransportConnectionInternal(this);
                ((MemoryBasedTransport)remoteClient.Transport).OnReceive(remoteClient, connection);
                return connection;
            }
        }

        private async void OnReceive(IQuasiHttpClient remoteClient, object connection)
        {
            try
            {
                await remoteClient.Receive(connection);
            }
            catch (Exception ex)
            {
                ErrorHandler?.Invoke(ex, "receive processing error");
            }
        }

        public async Task ReleaseConnection(object connection)
        {
            Task releaseTask = null;
            lock (EventLoop)
            {
                if (connection is MemoryBasedTransportConnectionInternal typedConnection)
                {
                    releaseTask = typedConnection.Release(EventLoop);
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
            lock (EventLoop)
            {
                readTask = typedConnection.ProcessReadRequest(EventLoop, this, data, offset, length);
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
            lock (EventLoop)
            {
                writeTask = typedConnection.ProcessWriteRequest(EventLoop, this, data, offset, length);
            }

            await writeTask;
        }
    }
}
