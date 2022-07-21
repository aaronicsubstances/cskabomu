using Kabomu.QuasiHttp.Transport;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class ReceiveTransferInternal
    {
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public int TimeoutMillis { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpProcessingOptions ProcessingOptions { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }
        public int MaxChunkSize { get; set; }
        public IQuasiHttpTransport Transport { get; set; }
        public object Connection { get; set; }
    }
}