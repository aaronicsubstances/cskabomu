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
        public async Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            int port = (int)connectivityParams.RemoteEndpoint;
            var tcpClient = new TcpClient();
            tcpClient.NoDelay = true;
            await tcpClient.ConnectAsync("localhost", port);
            var response = new DefaultConnectionAllocationResponse
            {
                Connection = tcpClient
            };
            return response;
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
