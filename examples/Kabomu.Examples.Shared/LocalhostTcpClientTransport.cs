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
        public async Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpSendOptions sendOptions)
        {
            int port = (int)remoteEndpoint;
            var clientSocket = new Socket(IPAddress.Loopback.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.NoDelay = true;
            await clientSocket.ConnectAsync(IPAddress.Loopback, port);
            var response = new DefaultConnectionAllocationResponse
            {
                Connection = new SocketWrapper(clientSocket)
            };
            return response;
        }

        public object GetWriter(object connection)
        {
            return LocalhostTcpServerTransport.GetWriterInternal(connection);
        }

        public object GetReader(object connection)
        {
            return LocalhostTcpServerTransport.GetReaderInternal(connection);
        }

        public Task ReleaseConnection(object connection)
        {
            return LocalhostTcpServerTransport.ReleaseConnectionInternal(connection);
        }
    }
}
