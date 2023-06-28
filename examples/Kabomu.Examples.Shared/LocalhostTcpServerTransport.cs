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
    public class LocalhostTcpServerTransport : IQuasiHttpServerTransport
    {
        private readonly object _mutex = new object();
        private readonly int _port;
        private TcpListener _tcpServer;

        public LocalhostTcpServerTransport()
        {
        }

        public LocalhostTcpServerTransport(int port)
        {
            _port = port;
        }

        public Task Start()
        {
            lock (_mutex)
            {
                if (_tcpServer == null)
                {
                    try
                    {
                        _tcpServer = new TcpListener(IPAddress.Loopback, _port);
                        _tcpServer.Start();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            _tcpServer?.Stop();
                        }
                        catch (Exception) { }
                        throw;
                    }
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

        public bool IsRunning()
        {
            lock (_mutex)
            {
                return _tcpServer != null;
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            Task<TcpClient> acceptTask;
            lock (_mutex)
            {
                if (_tcpServer == null)
                {
                    throw new InvalidOperationException("transport not started");
                }
                acceptTask = _tcpServer.AcceptTcpClientAsync();
            }
            var tcpClient = await acceptTask;
            tcpClient.NoDelay = true;
            return new DefaultConnectionAllocationResponse
            {
                Connection = tcpClient
            };
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
