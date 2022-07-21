using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp.Server
{
    public interface IQuasiHttpProcessingOptions
    {
        int TimeoutMillis { get; set; }
        int MaxChunkSize { get; set; }
        IDictionary<string, object> RequestEnvironment { get; set; }
    }
}
