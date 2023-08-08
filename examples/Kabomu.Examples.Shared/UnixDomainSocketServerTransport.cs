using Kabomu.QuasiHttp.Server;
using Kabomu.QuasiHttp.Transport;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketServerTransport : IQuasiHttpServerTransport
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();
        private readonly Socket _serverSocket;

        public UnixDomainSocketServerTransport(string path)
        {
            path = Path.Combine(Path.GetTempPath(), path);
            File.Delete(path); // recommended way of avoiding error
            // System.Net.Sockets.SocketException(10048): Only one usage of each socket 
            // address(protocol / network address / port) is normally permitted.
            _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            _serverSocket.Bind(new UnixDomainSocketEndPoint(path));
        }

        public StandardQuasiHttpServer Server { get; set; }

        public Task Start()
        {
            _serverSocket.Listen();
            // don't wait.
            _ = AcceptConnections();
            return Task.CompletedTask;
        }

        public async Task Stop()
        {
            _serverSocket.Dispose();
            await Task.Delay(1_000);
        }

        private async Task AcceptConnections()
        {
            try
            {
                while (true)
                {
                    var socket = await _serverSocket.AcceptAsync();
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
                await Server.AcceptConnection(
                    new DefaultConnectionAllocationResponse
                    {
                        Connection = new SocketWrapper(socket)
                    }
                );
            }
            catch (Exception ex)
            {
                LOG.Warn(ex, "connection processing error");
            }
        }

        public object GetWriter(object connection)
        {
            return GetWriterInternal(connection);
        }

        public object GetReader(object connection)
        {
            return GetReaderInternal(connection);
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static object GetWriterInternal(object connection)
        {
            return ((SocketWrapper)connection).Writer;
        }

        internal static object GetReaderInternal(object connection)
        {
            return ((SocketWrapper)connection).Reader;
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var socket = ((SocketWrapper)connection).Socket;
            socket.Dispose();
            return Task.CompletedTask;
        }
    }
}
