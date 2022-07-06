using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        public MemoryBasedClientTransport()
        {
        }

        public string LocalEndpoint { get; set; }
        public IMemoryBasedTransportHub Hub { get; set; }

        public Task<bool> CanProcessSendRequestDirectly()
        {
            return Hub.CanProcessSendRequestDirectly();
        }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            return Hub.ProcessSendRequest(LocalEndpoint, connectionAllocationInfo, request);
        }

        public Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            return Hub.AllocateConnection(LocalEndpoint, connectionRequest);
        }

        public Task ReleaseConnection(object connection)
        {
            return MemoryBasedServerTransport.ReleaseConnectionInternal(connection);
        }

        public Task<int> ReadBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.ReadBytesInternal(false, connection, data, offset, length);
        }

        public Task WriteBytes(object connection, byte[] data, int offset, int length)
        {
            return MemoryBasedServerTransport.WriteBytesInternal(false, connection, data, offset, length);
        }
    }
}
