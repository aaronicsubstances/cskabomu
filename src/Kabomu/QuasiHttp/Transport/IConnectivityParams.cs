using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public interface IConnectivityParams
    {
        object RemoteEndpoint { get; }
        IDictionary<string, object> ExtraParams { get; }
    }
}
