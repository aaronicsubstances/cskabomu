using Kabomu.Abstractions;
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
        public Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix,
                SocketType.Stream, ProtocolType.Unspecified);
            var connection = new SocketConnection(socket, true,
                sendOptions, DefaultSendOptions);
            var ongoingConnectionTask = socket.ConnectAsync(
                new UnixDomainSocketEndPoint(path));
            var connectionAllocationResponse = new DefaultConnectionAllocationResponse
            {
                Connection = connection,
                ConnectTask = ongoingConnectionTask
            };
            return Task.FromResult<IConnectionAllocationResponse>(
                connectionAllocationResponse);
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

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
