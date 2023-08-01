using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        public IDictionary<object, MemoryBasedServerTransport> Servers { get; set; }

        public Task<IConnectionAllocationResponse> AllocateConnection(IConnectivityParams connectivityParams)
        {
            if (!Servers.ContainsKey(connectivityParams.RemoteEndpoint))
            {
                return Task.FromResult<IConnectionAllocationResponse>(null);
            }
            var server = Servers[connectivityParams.RemoteEndpoint];
            if (server == null)
            {
                return Task.FromResult<IConnectionAllocationResponse>(
                    new DefaultConnectionAllocationResponse());
            }
            var connection = new MemoryBasedTransportConnectionInternal(
                ProtocolUtilsInternal.GetEnvVarAsBoolean(
                    connectivityParams.ExtraParams,
                    TransportUtils.ConnectivityParamFireAndForget));
            IConnectionAllocationResponse c = new DefaultConnectionAllocationResponse
            {
                Connection = connection,
                Environment = connectivityParams.ExtraParams
            };
            server.AcceptConnectionFunc.Invoke(c);
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
