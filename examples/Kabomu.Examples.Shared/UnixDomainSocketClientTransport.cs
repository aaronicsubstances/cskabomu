using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class UnixDomainSocketClientTransport : IQuasiHttpClientTransport
    {
        public async Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix,
                SocketType.Stream, ProtocolType.Unspecified);
            path = Path.Combine(Path.GetTempPath(), path);
            var connection = new SocketConnection(socket, true,
                sendOptions, DefaultSendOptions);
            var mainTask = socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
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
            return ((SocketConnection)connection).Release(
                responseStreamingEnabled);
        }

        public Task Write(IQuasiHttpConnection connection, bool isResponse,
            IEncodedQuasiHttpEntity entity)
        {
            return ((SocketConnection)connection).Write(isResponse, entity);
        }

        public Task<IEncodedQuasiHttpEntity> Read(
            IQuasiHttpConnection connection,
            bool isResponse)
        {
            return ((SocketConnection)connection).Read(
                isResponse);
        }

        public Task<Stream> ApplyResponseBuffering(IQuasiHttpConnection connection, Stream body)
        {
            return ((SocketConnection)connection).ApplyResponseBuffering(
                body);
        }
    }
}
