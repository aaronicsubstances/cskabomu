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
    public class UnixDomainSocketClientTransport : IQuasiHttpClientTransport
    {
        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix,
                SocketType.Stream, ProtocolType.Unspecified);
            var connection = new SocketConnection(socket, path,
                sendOptions, DefaultSendOptions);
            return Task.FromResult<IQuasiHttpConnection>(connection);
        }

        public async Task EstablishConnection(IQuasiHttpConnection connection)
        {
            var socketConnection = (SocketConnection)connection;
            var path = (string)socketConnection.ClientPortOrPath;
            await socketConnection.Socket.ConnectAsync(
                new UnixDomainSocketEndPoint(path));
        }

        public Task ReleaseConnection(IQuasiHttpConnection connection,
            IQuasiHttpResponse response)
        {
            return ((SocketConnection)connection).Release(
                response);
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
