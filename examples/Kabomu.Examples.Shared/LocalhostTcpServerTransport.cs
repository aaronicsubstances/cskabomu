using Kabomu.Concurrency;
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
        private readonly int _port;
        private TcpListener _tcpServer;

        public LocalhostTcpServerTransport()
        {
            MutexApi = new LockBasedMutexApi();
        }

        public IMutexApi MutexApi { get; set; }

        public LocalhostTcpServerTransport(int port)
        {
            _port = port;
        }

        public async Task Start()
        {
            using (await MutexApi.Synchronize())
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
        }

        public async Task Stop()
        {
            using (await MutexApi.Synchronize())
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
        }

        public async Task<bool> IsRunning()
        {
            using (await MutexApi.Synchronize())
            {
                return _tcpServer != null;
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            Task<TcpClient> acceptTask;
            using (await MutexApi.Synchronize())
            {
                if (_tcpServer == null)
                {
                    throw new TransportNotStartedException();
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
