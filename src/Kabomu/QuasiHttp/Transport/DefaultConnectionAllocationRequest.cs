using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public class DefaultConnectionAllocationRequest : IConnectionAllocationRequest
    {
        public object RemoteEndpoint { get; set; }
        public IDictionary<string, object> Environment { get; set; }
        public IMutexApi ConnectionMutexApi { get; set; }
    }
}
