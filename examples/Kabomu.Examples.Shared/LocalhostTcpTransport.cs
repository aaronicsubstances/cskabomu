using Kabomu.Common;
using Kabomu.Common.Transports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class LocalhostTcpTransport : IQuasiHttpTransport
    {
        private readonly TcpListener _tcpServer;

        public LocalhostTcpTransport(int port)
        {
            _tcpServer = new TcpListener(IPAddress.Loopback, port);
        }

        public bool DirectSendRequestProcessingEnabled => false;

        public async Task Start()
        {
            _tcpServer.Start();
        }

        public async Task Stop()
        {
            _tcpServer.Stop();
        }

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

        public async Task ReleaseConnection(object connection, bool wasReceived)
        {
            var tcpClient = (TcpClient)connection;
            tcpClient.Dispose();
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            return networkStream.WriteAsync(data, offset, length);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            return networkStream.ReadAsync(data, offset, length);
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            var tcpClient = await _tcpServer.AcceptTcpClientAsync();
            tcpClient.NoDelay = true;
            return new DefaultConnectionAllocationResponse
            {
                Connection = tcpClient
            };
        }
    }
}
