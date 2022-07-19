using Kabomu.Concurrency;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpProcessingOptions
    {
        int OverallReqRespTimeoutMillis { get; set; }
        int MaxChunkSize { get; set; }
        IDictionary<string, object> RequestEnvironment { get; set; }
    }
}
