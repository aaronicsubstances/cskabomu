using Kabomu.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeClientTransport : IQuasiHttpClientTransport
    {
        public Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpProcessingOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path,
                PipeDirection.InOut, PipeOptions.Asynchronous);
            var connection = new DuplexStreamConnection(pipeClient, true,
                sendOptions, DefaultSendOptions);
            var ongoingConnectionTask = pipeClient.ConnectAsync();
            var connectionAllocationResponse = new DefaultConnectionAllocationResponse
            {
                Connection = connection,
                ConnectTask = ongoingConnectionTask
            };
            return Task.FromResult<IConnectionAllocationResponse>(
                connectionAllocationResponse);
        }

        public IQuasiHttpProcessingOptions DefaultSendOptions { get; set; }

        public Task ReleaseConnection(IQuasiHttpConnection connection,
            bool responseStreamingEnabled)
        {
            return ((DuplexStreamConnection)connection).Release(
                responseStreamingEnabled);
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
