using Kabomu.Concurrency;
using System.Collections.Generic;

namespace Kabomu.QuasiHttp.Client
{
    public class DefaultQuasiHttpSendOptions : IQuasiHttpSendOptions
    {
        public IDictionary<string, object> ConnectivityParams { get; set; }
        public int TimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseStreamingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
    }
}