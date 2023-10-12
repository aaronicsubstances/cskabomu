using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeClientTransport : IQuasiHttpClientTransport
    {
        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task<IQuasiHttpConnection> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            var connection = new DuplexStreamConnection(pipeClient, true,
                sendOptions, DefaultSendOptions);
            return Task.FromResult<IQuasiHttpConnection>(connection);
        }

        public async Task EstablishConnection(IQuasiHttpConnection connection)
        {
            var socketConnection = (DuplexStreamConnection)connection;
            await ((NamedPipeClientStream)socketConnection.Stream).ConnectAsync();
        }

        public Task ReleaseConnection(IQuasiHttpConnection connection,
            IQuasiHttpResponse response)
        {
            return ((DuplexStreamConnection)connection).Release(
                response);
        }

        public Stream GetReadableStream(IQuasiHttpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Stream;
        }

        public Stream GetWritableStream(IQuasiHttpConnection connection)
        {
            return ((DuplexStreamConnection)connection).Stream;
        }
    }
}
