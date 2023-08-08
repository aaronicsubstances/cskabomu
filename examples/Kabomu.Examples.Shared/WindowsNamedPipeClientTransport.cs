using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class WindowsNamedPipeClientTransport : IQuasiHttpClientTransport
    {
        public async Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpSendOptions sendOptions)
        {
            var path = (string)remoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync();
            var response = new DefaultConnectionAllocationResponse
            {
                Connection = pipeClient
            };
            return response;
        }

        public object GetWriter(object connection)
        {
            return WindowsNamedPipeServerTransport.GetWriterInternal(connection);
        }

        public object GetReader(object connection)
        {
            return WindowsNamedPipeServerTransport.GetReaderInternal(connection);
        }

        public Task ReleaseConnection(object connection)
        {
            return WindowsNamedPipeServerTransport.ReleaseConnectionInternal(connection);
        }
    }
}
