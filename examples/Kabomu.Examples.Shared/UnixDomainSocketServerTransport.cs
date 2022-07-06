using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
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
        private readonly string _path;
        private Socket _serverSocket;

        public UnixDomainSocketServerTransport()
        {
            MutexApi = new LockBasedMutexApi(new object());
        }

        public IMutexApi MutexApi { get; set; }

        public UnixDomainSocketServerTransport(string path)
        {
            path = Path.Combine(Path.GetTempPath(), path);
            File.Delete(path); // recommended way of avoiding error
            // System.Net.Sockets.SocketException(10048): Only one usage of each socket 
            // address(protocol / network address / port) is normally permitted.
            _path = path;
        }

        public async Task Start()
        {
            using (await MutexApi.Synchronize())
            {
                if (_serverSocket == null)
                {
                    try
                    {
                        _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        _serverSocket.Bind(new UnixDomainSocketEndPoint(_path));
                        _serverSocket.Listen(5);
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
        }

        public async Task Stop()
        {
            using (await MutexApi.Synchronize())
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
        }

        public async Task<bool> IsRunning()
        {
            using (await MutexApi.Synchronize())
            {
                return _serverSocket != null;
            }
        }

        public async Task<IConnectionAllocationResponse> ReceiveConnection()
        {
            Task<Socket> acceptTask;
            using (await MutexApi.Synchronize())
            {
                if (_serverSocket == null)
                {
                    throw new Exception("transport not started");
                }
                acceptTask = _serverSocket.AcceptAsync();
            }
            var socket = await acceptTask;
            return new DefaultConnectionAllocationResponse
            {
                Connection = socket
            };
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
