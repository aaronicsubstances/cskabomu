using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Transport
{
    public class DefaultConnectivityParams : IConnectivityParams
    {
        public object RemoteEndpoint { get; set; }
        public IDictionary<string, object> ExtraParams { get; set; }
    }
}
