using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketClientTransport : IQuasiHttpClientTransport
    {
        public Task<bool> CanProcessSendRequestDirectly() => Task.FromResult(false);

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            throw new NotImplementedException();
        }

        public async Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            var path = (string)connectionRequest.RemoteEndpoint;
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            path = Path.Combine(Path.GetTempPath(), path);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
            return socket;
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var socket = (Socket)connection;
            socket.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (Socket)connection;
            return networkStream.ReceiveAsync(new Memory<byte>(data, offset, length), SocketFlags.None).AsTask();
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(connection, data, offset, length);
        }

        internal static async Task WriteBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (Socket)connection;
            int totalBytesSent = 0;
            while (totalBytesSent < length)
            {
                int bytesSent = await networkStream.SendAsync(
                    new ReadOnlyMemory<byte>(data, offset + totalBytesSent, length - totalBytesSent), SocketFlags.None);
                totalBytesSent += bytesSent;
            }
        }
    }
}

