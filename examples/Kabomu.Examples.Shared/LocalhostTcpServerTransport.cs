using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
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
        //private readonly object _mutex = new object();
        private readonly Socket _tcpServer;

        public LocalhostTcpServerTransport(int port)
        {
            _tcpServer = new Socket(IPAddress.Loopback.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);
            _tcpServer.Bind(new IPEndPoint(IPAddress.Loopback, port));
        }

        public StandardQuasiHttpServer Server { get; set; }

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
                //socket.NoDelay = true;
                await Server.AcceptConnection(
                    new DefaultConnectionAllocationResponse
                    {
                        Connection = socket
                    }
                );
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "connection processing error");
            }
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var networkStream = (Socket)connection;
            networkStream.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static async Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (Socket)connection;
            return await networkStream.ReceiveAsync(new Memory<byte>(data, offset, length), SocketFlags.None);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(connection, data, offset, length);
        }

        internal static async Task WriteBytesInternal(object connection, byte[] data, int offset, int length)
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
    }
}
