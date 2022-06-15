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

        public async Task<IQuasiHttpResponse> ProcessSendRequestAsync(object remoteEndpoint, IQuasiHttpRequest request)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }
            if (request == null)
            {
                throw new ArgumentException("null request");
            }

            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            var remoteClient = Hub.Clients[remoteEndpoint];
            return await remoteClient.Application.ProcessRequestAsync(request);
        }

        public async Task<object> AllocateConnectionAsync(object remoteEndpoint)
        {
            if (remoteEndpoint == null)
            {
                throw new ArgumentException("null remote endpoint");
            }

            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            var remoteClient = Hub.Clients[remoteEndpoint];
            var connection = new MemoryBasedTransportConnectionInternal(this);
            await remoteClient.ReceiveAsync(connection);
            return connection;
        }

        public async Task ReleaseConnectionAsync(object connection)
        {
            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            if (connection is MemoryBasedTransportConnectionInternal typedConnection)
            {
                await typedConnection.ReleaseAsync(EventLoop);
            }
        }

        public async Task<int> ReadBytesAsync(object connection, byte[] data, int offset, int length)
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

            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            return await typedConnection.ProcessReadRequestAsync(EventLoop, this, data, offset, length);
        }

        public async Task WriteBytesAsync(object connection, byte[] data, int offset, int length)
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

            if (EventLoop.IsMutexRequired(out Task mt)) await mt;

            await typedConnection.ProcessWriteRequestAsync(EventLoop, this, data, offset, length);
        }
    }
}
