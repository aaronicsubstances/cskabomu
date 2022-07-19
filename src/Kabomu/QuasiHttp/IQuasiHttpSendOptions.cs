using Kabomu.Concurrency;
using System.Collections.Generic;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpSendOptions
    {
        int OverallReqRespTimeoutMillis { get; }
        int MaxChunkSize { get; }
        IDictionary<string, object> RequestEnvironment { get; }
    }
}