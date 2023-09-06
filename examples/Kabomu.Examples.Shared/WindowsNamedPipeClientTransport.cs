using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeClientTransport : IQuasiHttpClientTransport
    {
        public async Task<IQuasiTcpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync();
            return new DuplexStreamConnection(pipeClient, true,
                sendOptions, DefaultSendOptions);
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public object GetWriter(IQuasiTcpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Writer;
        }

        public object GetReader(IQuasiTcpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Reader;
        }

        public Task ReleaseConnection(IQuasiTcpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Release();
        }

        public Task Write(IQuasiTcpConnection connection, bool isResponse,
            byte[] encodedHeaders, object requestBodyReader)
        {
            return ((DuplexStreamConnection)connection).Write(isResponse,
                encodedHeaders, requestBodyReader);
        }

        public Task<IEncodedReadRequest> Read(
            IQuasiTcpConnection connection,
            bool isResponse)
        {
            return ((DuplexStreamConnection)connection).Read(
                isResponse);
        }
    }
}
