using Kabomu.Common;
using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Kabomu.Examples.Shared
{
    public class LocalhostTcpTransport : IQuasiHttpTransport
    {
        private readonly TcpListener _tcpServer;

        public LocalhostTcpTransport(int port)
        {
            _tcpServer = new TcpListener(IPAddress.Loopback, port);
            MaxMessageOrChunkSize = 8192;
        }

        public int MaxMessageOrChunkSize { get; set; }

        public bool IsByteOriented => true;

        public bool DirectSendRequestProcessingEnabled => false;

        public KabomuQuasiHttpClient Upstream { get; set; }

        public UncaughtErrorCallback ErrorHandler { get; set; }

        public async void Start()
        {
            _tcpServer.Start();
            while (true)
            {
                try
                {
                    var tcpClient = await _tcpServer.AcceptTcpClientAsync();
                    Upstream.OnReceive(tcpClient);
                }
                catch (Exception e)
                {
                    if (e is ObjectDisposedException)
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
            _tcpServer.Stop();
        }

        public void ProcessSendRequest(object remoteEndpoint, IQuasiHttpRequestMessage request, 
            Action<Exception, IQuasiHttpResponseMessage> cb)
        {
            throw new NotImplementedException();
        }

        public async void AllocateConnection(object remoteEndpoint, Action<Exception, object> cb)
        {
            int port = (int)remoteEndpoint;
            var tcpClient = new TcpClient();
            try
            {
                await tcpClient.ConnectAsync("localhost", port);
                cb.Invoke(null, tcpClient);
            }
            catch (Exception e)
            {
                tcpClient.Dispose();
                cb.Invoke(e, null);
            }
        }

        public void ReleaseConnection(object connection)
        {
            var tcpClient = (TcpClient)connection;
            tcpClient.Dispose();
        }

        public async void WriteBytes(object connection, byte[] data, int offset, int length, Action<Exception> cb)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            try
            {
                await networkStream.WriteAsync(data, offset, length);
                cb.Invoke(null);
            }
            catch (Exception e)
            {
                cb.Invoke(e);
            }
        }

        public void SendMessage(object connection, byte[] data, int offset, int length, 
            Action<Action<bool>> cancellationEnquirer, Action<Exception> cb)
        {
            throw new NotImplementedException();
        }

        public async void ReadBytes(object connection, byte[] data, int offset, int length, Action<Exception, int> cb)
        {
            var tcpClient = (TcpClient)connection;
            Stream networkStream = tcpClient.GetStream();
            try
            {
                int bytesRead = await networkStream.ReadAsync(data, offset, length);
                cb.Invoke(null, bytesRead);
            }
            catch (Exception e)
            {
                cb.Invoke(e, 0);
            }
        }
    }
}
