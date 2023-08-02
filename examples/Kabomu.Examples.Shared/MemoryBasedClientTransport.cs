using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Examples.Shared
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        public IDictionary<object, MemoryBasedServerTransport> Servers { get; set; }

        public Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpSendOptions sendOptions)
        {
            var server = Servers[remoteEndpoint];
            var connection = new MemoryBasedTransportConnectionInternal();
            IConnectionAllocationResponse c = new DefaultConnectionAllocationResponse
            {
                Connection = connection,
                Environment = sendOptions?.ExtraConnectivityParams
            };
            _ = server.AcceptConnection(c);
            return Task.FromResult(c);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.ProcessReadRequest(false, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.ProcessWriteRequest(false, data, offset, length);
        }

        public Task ReleaseConnection(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.Release();
        }
    }
}
