using Kabomu.QuasiHttp.EntityBody;
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
        public async Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            var path = (string)connectivityParams.RemoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync();
            var response = new DefaultConnectionAllocationResponse
            {
                Connection = pipeClient
            };
            return response;
        }

        public Task ReleaseConnection(object connection)
        {
            return WindowsNamedPipeServerTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return WindowsNamedPipeServerTransport.ReadBytesInternal(connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WindowsNamedPipeServerTransport.WriteBytesInternal(connection, data, offset, length);
        }

        public Task<bool> TrySerializeBody(object connection, byte[] prefix, IQuasiHttpBody body)
        {
            return WindowsNamedPipeServerTransport.TrySerializeBodyInternal(this, connection, prefix, body);
        }

        public Task<IQuasiHttpBody> DeserializeBody(object connection, long contentLength)
        {
            return WindowsNamedPipeServerTransport.DeserializeBodyInternal(this, connection, contentLength);
        }
    }
}
