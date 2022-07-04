using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Transport
{
    public class MemoryBasedTransportHub
    {
        public MemoryBasedTransportHub()
        {
            MutexApi = new LockBasedMutexApi(new object());
            Servers = new Dictionary<object, MemoryBasedServerTransport>();
        }

        public IMutexApi MutexApi { get; set; }
        public Dictionary<object, MemoryBasedServerTransport> Servers { get; }

        public async Task<MemoryBasedServerTransport> GetServer(object remoteEndpoint)
        {
            using (await MutexApi.Synchronize())
            {
                return Servers[remoteEndpoint];
            }
        }
    }
}
