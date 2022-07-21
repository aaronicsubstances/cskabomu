using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Server
{
    public class DefaultQuasiHttpProcessingOptions : IQuasiHttpProcessingOptions
    {
        public int TimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
    }
}
