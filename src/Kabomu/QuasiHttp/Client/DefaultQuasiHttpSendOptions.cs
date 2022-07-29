using System.Collections.Generic;

namespace Kabomu.QuasiHttp.Client
{
    /// <summary>
    /// Implementation of <see cref="IQuasiHttpSendOptions"/> providing mutable versions of
    /// all properties in interface.
    /// </summary>
    public class DefaultQuasiHttpSendOptions : IQuasiHttpSendOptions
    {
        public IDictionary<string, object> ExtraConnectivityParams { get; set; }
        public int TimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public bool? ResponseStreamingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
    }
}