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
        private readonly object _mutex = new object();
        private readonly int _port;
        private TcpListener _tcpServer;

        public LocalhostTcpServerTransport(int port)
        {
            _port = port;
        }

        public StandardQuasiHttpServer Server { get; set; }

        public Task Start()
        {
            lock (_mutex)
            {
                if (_tcpServer == null)
                {
                    _tcpServer = new TcpListener(IPAddress.Loopback, _port);
                    _tcpServer.Start();
                    _ = ServerUtils.AcceptConnections(ReceiveConnection,
                            IsDoneRunning);
                }
            }
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            lock (_mutex)
            {
                try
                {
                    _tcpServer?.Stop();
                }
                finally
                {
                    _tcpServer = null;
                }
            }
            return Task.CompletedTask;
        }

        private Task<bool> IsDoneRunning(Exception latestError)
        {
            if (latestError != null)
            {
                LOG.Warn(latestError, "connection receive error");
                return Task.FromResult(true);
            }
            lock (_mutex)
            {
                return Task.FromResult(_tcpServer == null);
            }
        }

        private async Task<bool> ReceiveConnection()
        {
            LOG.Info("accepting...");
            var tcpClient = await _tcpServer.AcceptTcpClientAsync();
            tcpClient.NoDelay = true;
            var c = new DefaultConnectionAllocationResponse
            {
                Connection = tcpClient
            };
            async Task ForwardConnection()
            {

                try
                {
                    await Server.AcceptConnection(c);
                }
                catch (Exception ex)
                {
                    LOG.Warn(ex, "connection processing error");
                }
            }
            _ = ForwardConnection();
            return true;
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var tcpClient = (TcpClient)connection;
            tcpClient.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            return networkStream.ReadAsync(data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(connection, data, offset, length);
        }

        internal static Task WriteBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            return networkStream.WriteAsync(data, offset, length);
        }
    }
}
