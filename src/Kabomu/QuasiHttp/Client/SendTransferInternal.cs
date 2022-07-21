using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    internal class SendTransferInternal
    {
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public ISendProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
        public object BypassCancellationHandle { get; set; }
        public DefaultConnectivityParams ConnectivityParams { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public IQuasiHttpSendOptions SendOptions { get; set; }
        public int TimeoutMillis { get; set; }
        public int MaxChunkSize { get; set; }
        public bool ResponseStreamingEnabled { get; set; }
        public int ResponseBodyBufferingSizeLimit { get; set; }
    }
}
