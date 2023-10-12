using Kabomu.Abstractions;
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
        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var port = (int)remoteEndpoint;
            var socket = new Socket(AddressFamily.InterNetworkV6,
                SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            var connection = new SocketConnection(socket, port,
                sendOptions, DefaultSendOptions);
            return Task.FromResult<IQuasiHttpConnection>(connection);
        }

        public async Task EstablishConnection(IQuasiHttpConnection connection)
        {
            var socketConnection = (SocketConnection)connection;
            var hostIp = IPAddress.Parse("::1");
            await socketConnection.Socket.ConnectAsync(
                hostIp, (int)socketConnection.ClientPortOrPath);
        }

        public Task ReleaseConnection(IQuasiHttpConnection connection,
            IQuasiHttpResponse response)
        {
            return ((SocketConnection)connection).Release(response);
        }

        public Stream GetReadableStream(IQuasiHttpConnection connection)
        {
            return ((SocketConnection)connection).Stream;
        }

        public Stream GetWritableStream(IQuasiHttpConnection connection)
        {
            return ((SocketConnection)connection).Stream;
        }
    }
}
