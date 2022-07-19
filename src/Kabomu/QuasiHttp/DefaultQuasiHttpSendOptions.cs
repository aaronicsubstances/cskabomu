using Kabomu.Concurrency;
using System.Collections.Generic;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpSendOptions : IQuasiHttpSendOptions
    {
        public int OverallReqRespTimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> ConnectivityParams { get; set; }
    }
}