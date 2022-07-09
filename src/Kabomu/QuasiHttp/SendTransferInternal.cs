using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class SendTransferInternal
    {
        public CancellationTokenSource TransferCancellationHandle { get; set; }
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public SendProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<IQuasiHttpResponse> CancellationTcs { get; set; }
    }
}
