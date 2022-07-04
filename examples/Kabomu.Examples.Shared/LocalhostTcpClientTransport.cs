using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class LocalhostTcpClientTransport : IQuasiHttpClientTransport
    {
        public Task<bool> CanProcessSendRequestDirectly() => Task.FromResult(false);

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            throw new NotImplementedException();
        }

        public async Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            int port = (int)connectionRequest.RemoteEndpoint;
            var tcpClient = new TcpClient();
            tcpClient.NoDelay = true;
            await tcpClient.ConnectAsync("localhost", port);
            return tcpClient;
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var tcpClient = (TcpClient)connection;
            tcpClient.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            return networkStream.ReadAsync(data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(connection, data, offset, length);
        }

        internal static Task WriteBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            return networkStream.WriteAsync(data, offset, length);
        }
    }
}
