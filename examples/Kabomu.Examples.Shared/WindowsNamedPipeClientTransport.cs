using Kabomu.QuasiHttp;
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
        public Task<bool> CanProcessSendRequestDirectly() => Task.FromResult(false);

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            throw new NotImplementedException();
        }

        public async Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            var path = (string)connectionRequest.RemoteEndpoint;
            var pipeClient = new NamedPipeClientStream(".", path, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipeClient.ConnectAsync();
            return pipeClient;
        }

        public Task ReleaseConnection(object connection)
        {
            return ReleaseConnectionInternal(connection);
        }

        internal static Task ReleaseConnectionInternal(object connection)
        {
            var pipeStream = (PipeStream)connection;
            pipeStream.Dispose();
            return Task.CompletedTask;
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return ReadBytesInternal(connection, data, offset, length);
        }

        internal static Task<int> ReadBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (PipeStream)connection;
            return networkStream.ReadAsync(data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return WriteBytesInternal(connection, data, offset, length);
        }

        internal static Task WriteBytesInternal(object connection, byte[] data, int offset, int length)
        {
            var networkStream = (PipeStream)connection;
            return networkStream.WriteAsync(data, offset, length);
        }
    }
}
