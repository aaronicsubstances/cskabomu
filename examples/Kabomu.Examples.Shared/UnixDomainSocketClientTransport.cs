using Kabomu.QuasiHttp;
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
        public async Task<IQuasiTcpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            path = Path.Combine(Path.GetTempPath(), path);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
            return new SocketConnection(socket, true,
                sendOptions, DefaultSendOptions);
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public object GetWriter(IQuasiTcpConnection connection)
        {
            return ((SocketConnection)connection).Writer;
        }

        public object GetReader(IQuasiTcpConnection connection)
        {
            return ((SocketConnection)connection).Reader;
        }

        public Task ReleaseConnection(IQuasiTcpConnection connection)
        {
            return ((SocketConnection)connection).Release();
        }

        public Task Write(IQuasiTcpConnection connection, bool isResponse,
            byte[] encodedHeaders, object requestBodyReader)
        {
            return ((SocketConnection)connection).Write(isResponse,
                encodedHeaders, requestBodyReader);
        }

        public Task<IEncodedReadRequest> Read(
            IQuasiTcpConnection connection,
            bool isResponse)
        {
            return ((SocketConnection)connection).Read(
                isResponse);
        }
    }
}
