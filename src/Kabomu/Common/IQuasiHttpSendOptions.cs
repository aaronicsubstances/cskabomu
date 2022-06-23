using System.Collections.Generic;

namespace Kabomu.Common
{
    public interface IQuasiHttpSendOptions
    {
        int OverallReqRespTimeoutMillis { get; }
        int MaxChunkSize { get; }
        IDictionary<string, object> RequestEnvironment { get; }
    }
}