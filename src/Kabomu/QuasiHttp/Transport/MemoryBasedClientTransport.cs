using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedClientTransport : IQuasiHttpClientTransport
    {
        private readonly Random _randGen = new Random();

        public MemoryBasedClientTransport()
        {
            MutexApi = new LockBasedMutexApi(new object());
        }

        public string LocalEndpoint { get; set; }
        public IMemoryBasedTransportHub Hub { get; set; }
        public double DirectSendRequestProcessingProbability { get; set; }
        public IMutexApi MutexApi { get; set; }

        public async Task<bool> CanProcessSendRequestDirectly()
        {
            using (await MutexApi.Synchronize())
            {
                return _randGen.NextDouble() < DirectSendRequestProcessingProbability;
            }
        }

        public Task<IQuasiHttpResponse> ProcessSendRequest(IQuasiHttpRequest request,
            IConnectionAllocationRequest connectionAllocationInfo)
        {
            return Hub.ProcessSendRequest(request, connectionAllocationInfo);
        }

        public Task<object> AllocateConnection(IConnectionAllocationRequest connectionRequest)
        {
            return Hub.AllocateConnection(this, connectionRequest);
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
