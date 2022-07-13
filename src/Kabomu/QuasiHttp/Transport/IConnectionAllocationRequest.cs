using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IConnectionAllocationRequest
    {
        object RemoteEndpoint { get; }
        IDictionary<string, object> ConnectivityParameters { get; }
        IMutexApi ProcessingMutexApi { get; }
    }
}
