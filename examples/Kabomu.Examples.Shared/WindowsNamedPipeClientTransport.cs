using Kabomu.Abstractions;
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
        public async Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync();
            return new DuplexStreamConnection(pipeClient, true,
                sendOptions, DefaultSendOptions);
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public object GetWriter(IQuasiHttpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Writer;
        }

        public object GetReader(IQuasiHttpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Reader;
        }

        public Task ReleaseConnection(IQuasiHttpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Release();
        }

        public Task Write(IQuasiHttpConnection connection, bool isResponse,
            byte[] encodedHeaders, object requestBodyReader)
        {
            return ((DuplexStreamConnection)connection).Write(isResponse,
                encodedHeaders, requestBodyReader);
        }

        public Task<IEncodedReadRequest> Read(
            IQuasiHttpConnection connection,
            bool isResponse)
        {
            return ((DuplexStreamConnection)connection).Read(
                isResponse);
        }
    }
}
