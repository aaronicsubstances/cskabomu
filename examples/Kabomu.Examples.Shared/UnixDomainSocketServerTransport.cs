using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using NLog;
using NLog.Fluent;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketServerTransport : IQuasiHttpServerTransport
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly object _mutex = new object();
        private readonly string _path;
        private Socket _serverSocket;

        public UnixDomainSocketServerTransport(string path)
        {
            path = Path.Combine(Path.GetTempPath(), path);
            File.Delete(path); // recommended way of avoiding error
            // System.Net.Sockets.SocketException(10048): Only one usage of each socket 
            // address(protocol / network address / port) is normally permitted.
            _path = path;
        }

        public StandardQuasiHttpServer Server { get; set; }

        public Task Start()
        {
            lock (_mutex)
            {
                if (_serverSocket == null)
                {
                    try
                    {
                        _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        _serverSocket.Bind(new UnixDomainSocketEndPoint(_path));
                        _serverSocket.Listen(5);
                        _ = ServerUtils.AcceptConnections(ReceiveConnection,
                                IsDoneRunning);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            _serverSocket?.Dispose();
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
                    _serverSocket?.Dispose();
                }
                finally
                {
                    _serverSocket = null;
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
                return Task.FromResult(_serverSocket == null);
            }
        }

        private async Task<bool> ReceiveConnection()
        {
            LOG.Info("accepting...");
            var socket = await _serverSocket.AcceptAsync();
            var connectionAllocRes = new DefaultConnectionAllocationResponse
            {
                Connection = socket
            };
            async Task ForwardConnection()
            {

                try
                {
                    await Server.AcceptConnection(connectionAllocRes);
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
            var socket = (Socket)connection;
            socket.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (Socket)connection;
            return networkStream.ReceiveAsync(new Memory<byte>(data, offset, length), SocketFlags.None).AsTask();
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
