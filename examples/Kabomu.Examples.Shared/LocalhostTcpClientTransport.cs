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
            return LocalhostTcpServerTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return LocalhostTcpServerTransport.ReadBytesInternal(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return LocalhostTcpServerTransport.WriteBytesInternal(connection, data, offset, length);
        }
    }
}
