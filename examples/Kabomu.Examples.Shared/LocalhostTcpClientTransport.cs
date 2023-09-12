using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class LocalhostTcpClientTransport : IQuasiHttpClientTransport
    {
        public async Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            int port = (int)remoteEndpoint;
            var socket = new Socket(AddressFamily.InterNetworkV6,
                SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            var connection = new SocketConnection(socket, true,
                sendOptions, DefaultSendOptions);
            var mainTask = socket.ConnectAsync(
                IPAddress.Parse("::1"), port);
            try
            {
                await MiscUtils.CompleteMainTask(mainTask, connection.TimeoutId?.Task);
            }
            catch (Exception)
            {
                try
                {
                    // don't wait.
                    _ = connection.Release(false);
                }
                catch (Exception) { } //ignore
                throw;
            }
            return connection;
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task ReleaseConnection(IQuasiHttpConnection connection,
            bool responseStreamingEnabled)
        {
            return ((SocketConnection)connection).Release(responseStreamingEnabled);
        }

        public Task Write(IQuasiHttpConnection connection, bool isResponse,
            byte[] encodedHeaders, Stream body)
        {
            return ((SocketConnection)connection).Write(isResponse,
                encodedHeaders, body);
        }

        public Task<Stream> Read(
            IQuasiHttpConnection connection,
            bool isResponse, List<byte[]> encodedHeadersReceiver)
        {
            return ((SocketConnection)connection).Read(
                isResponse, encodedHeadersReceiver);
        }
    }
}
