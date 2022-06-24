using Kabomu.Common;
using Kabomu.Common.Transports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketTransport : IQuasiHttpTransport
    {
        private readonly Socket _serverSocket;

        public UnixDomainSocketTransport(string path)
        {
            path = Path.Combine(Path.GetTempPath(), path);
            File.Delete(path); // recommended way of avoiding error
            // System.Net.Sockets.SocketException(10048): Only one usage of each socket 
            // address(protocol / network address / port) is normally permitted.

            _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            _serverSocket.Bind(new UnixDomainSocketEndPoint(path));
        }

        public bool DirectSendRequestProcessingEnabled => false;

        public async Task Start()
        {
            _serverSocket.Listen(5);
        }

        public async Task Stop()
        {
            _serverSocket.Dispose();
        }

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

        public async Task ReleaseConnection(object connection)
        {
            var socket = (Socket)connection;
            socket.Dispose();
        }

        public async Task WriteBytes(object connection, byte[] data, int offset, int length)
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

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (Socket)connection;
            return networkStream.ReceiveAsync(new Memory<byte>(data, offset, length), SocketFlags.None).AsTask();
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            var socket = await _serverSocket.AcceptAsync();
            return new DefaultConnectionAllocationResponse
            {
                Connection = socket
            };
        }
    }
}
