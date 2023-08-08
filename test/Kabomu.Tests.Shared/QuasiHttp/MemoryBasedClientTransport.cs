using Kabomu.Common;
using Kabomu.QuasiHttp;
using Kabomu.QuasiHttp.Client;
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

        public IQuasiHttpSendOptions ActualSendOptions { get; set; }
        public object ActualRemoteEndpoint { get; set; }

        public Task<IConnectionAllocationResponse> AllocateConnection(
            object remoteEndpoint, IQuasiHttpSendOptions sendOptions)
        {
            ActualRemoteEndpoint = remoteEndpoint;
            ActualSendOptions = sendOptions;
            if (!Servers.ContainsKey(remoteEndpoint))
            {
                return Task.FromResult<IConnectionAllocationResponse>(null);
            }
            var server = Servers[remoteEndpoint];
            if (server == null)
            {
                return Task.FromResult<IConnectionAllocationResponse>(
                    new DefaultConnectionAllocationResponse());
            }
            var connection = new MemoryBasedTransportConnectionInternal(
                ProtocolUtilsInternal.GetEnvVarAsBoolean(
                    sendOptions?.ExtraConnectivityParams,
                    TransportUtils.ConnectivityParamFireAndForget));
            IConnectionAllocationResponse c = new DefaultConnectionAllocationResponse
            {
                Connection = connection,
                Environment = sendOptions?.ExtraConnectivityParams
            };
            server.AcceptConnectionFunc.Invoke(c);
            return Task.FromResult(c);
        }

        public object GetReader(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return new LambdaBasedCustomReader
            {
                ReadFunc = (data, offset, length) =>
                    typedConnection.ProcessReadRequest(false, data, offset, length)
            };
        }

        public object GetWriter(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return new LambdaBasedCustomWriter
            {
                WriteFunc = (data, offset, length) =>
                    typedConnection.ProcessWriteRequest(false, data, offset, length)
            };
        }

        public Task ReleaseConnection(object connection)
        {
            var typedConnection = (MemoryBasedTransportConnectionInternal)connection;
            return typedConnection.Release();
        }
    }
}
