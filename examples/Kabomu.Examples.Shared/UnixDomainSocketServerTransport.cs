using Kabomu.Concurrency;
using Kabomu.QuasiHttp;
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
            return UnixDomainSocketClientTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return UnixDomainSocketClientTransport.ReadBytesInternal(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return UnixDomainSocketClientTransport.WriteBytesInternal(connection, data, offset, length);
        }
    }
}
