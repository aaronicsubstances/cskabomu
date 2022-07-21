using System.Collections.Generic;

namespace Kabomu.QuasiHttp.Client
{
    public interface IQuasiHttpSendOptions
    {
        IDictionary<string, object> ConnectivityParams { get; }
        int TimeoutMillis { get; }
        int MaxChunkSize { get; }
        bool ResponseStreamingEnabled { get; }
        int ResponseBodyBufferingSizeLimit { get; }
    }
}