using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketTransport : IQuasiHttpTransport
    {
        private readonly Socket _serverSocket;

        public UnixDomainSocketTransport(string path)
        {
            path = Path.Combine(Path.GetTempPath(), path);
            File.Delete(path); // recommended way of avoiding error
            // System.Net.Sockets.SocketException(10048): Only one usage of each socket 
            // address(protocol / network address / port) is normally permitted.

            _serverSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            _serverSocket.Bind(new UnixDomainSocketEndPoint(path));
            MaxChunkSize = 8192;
        }

        public int MaxChunkSize { get; set; }

        public bool DirectSendRequestProcessingEnabled => false;

        public KabomuQuasiHttpClient Upstream { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public async void Start()
        {
            _serverSocket.Listen(5);
            while (true)
            {
                try
                {
                    var socket = await _serverSocket.AcceptAsync();
                    Upstream.OnReceive(socket);
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException)
                    {
                        break;
                    }
                    else if (e is SocketException se && se.SocketErrorCode == SocketError.OperationAborted)
                    {
                        break;
                    }
                    else
                    {
                        ErrorHandler?.Invoke(e, "error encountered during receiving");
                    }
                }
            }
        }

        public void Stop()
        {
            _serverSocket.Dispose();
        }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequest request,
            Action<Exception, IQuasiHttpResponse> cb)
        {
            throw new NotImplementedException();
        }

        public async void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            try
            {
                path = Path.Combine(Path.GetTempPath(), path);
                await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
                cb.Invoke(null, socket);
            }
            catch (Exception e)
            {
                socket.Dispose();
                cb.Invoke(e, null);
            }
        }

        public void ReleaseConnection(object connection)
        {
            var socket = (Socket)connection;
            socket.Dispose();
        }

        public async void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var networkStream = (Socket)connection;
            try
            {
                int totalBytesSent = 0;
                while (totalBytesSent < length)
                {
                    int bytesSent = await networkStream.SendAsync(
                        new ReadOnlyMemory<byte>(data, offset + totalBytesSent, length - totalBytesSent), SocketFlags.None);
                    totalBytesSent += bytesSent;
                }
                cb.Invoke(null);
            }
            catch (Exception e)
            {
                cb.Invoke(e);
            }
        }

        public async void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var networkStream = (Socket)connection;
            try
            {
                int bytesRead = await networkStream.ReceiveAsync(
                    new Memory<byte>(data, offset, length), SocketFlags.None);
                cb.Invoke(null, bytesRead);
            }
            catch (Exception e)
            {
                cb.Invoke(e, 0);
            }
        }
    }
}
