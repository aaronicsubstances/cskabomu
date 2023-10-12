using Kabomu.Abstractions;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class LocalhostTcpServerTransport : IQuasiHttpServerTransport
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly Socket _tcpServer;

        public LocalhostTcpServerTransport(int port)
        {
            _tcpServer = new Socket(AddressFamily.InterNetworkV6,
                SocketType.Stream, ProtocolType.Tcp);
            _tcpServer.Bind(new IPEndPoint(IPAddress.Parse("::1"), port));
        }

        public StandardQuasiHttpServer QuasiHttpServer { get; set; }

        public IQuasiHttpProcessingOptions DefaultProcessingOptions { get; set; }

        public Task Start()
        {
            _tcpServer.Listen();
            // don't wait.
            _ = AcceptConnections();
            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            _tcpServer.Dispose();
            await Task.Delay(1_000);
        }

        private async Task AcceptConnections()
        {
            try
            {
                while (true)
                {
                    var socket = await _tcpServer.AcceptAsync();
                    // don't wait.
                    _ = ReceiveConnection(socket);
                }
            }
            catch (Exception e)
            {
                if (e is SocketException s &&
                    s.SocketErrorCode == SocketError.OperationAborted)
                {
                    LOG.Info("connection accept ended");
                }
                else
                {
                    LOG.Warn(e, "connection accept error");
                }
            }
        }

        private async Task ReceiveConnection(Socket socket)
        {
            try
            {
                socket.NoDelay = true;
                var connection = new SocketConnection(socket, null,
                    DefaultProcessingOptions);
                await QuasiHttpServer.AcceptConnection(connection);
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "connection processing error");
            }
        }

        public Task ReleaseConnection(IQuasiHttpConnection connection)
        {
            return ((SocketConnection)connection).Release(null);
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
