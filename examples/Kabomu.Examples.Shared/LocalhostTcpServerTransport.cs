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
        private readonly object _lock = new object();
        private readonly int _port;
        private TcpListener _tcpServer;

        public LocalhostTcpServerTransport(int port)
        {
            _port = port;
        }

        public Task Start()
        {
            lock (_lock)
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
            lock (_lock)
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

        public bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _tcpServer != null;
                }
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            Task<TcpClient> acceptTask;
            lock (_lock)
            {
                if (_tcpServer == null)
                {
                    throw new Exception("transport not started");
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
            return LocalhostTcpClientTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return LocalhostTcpClientTransport.ReadBytesInternal(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return LocalhostTcpClientTransport.WriteBytesInternal(connection, data, offset, length);
        }
    }
}
