using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal interface ITransferProtocolInternal
    {
        IParentTransferProtocolInternal Parent { get; set; }
        object Connection { get; set; }
        bool IsAborted { get; set; }
        int TimeoutMillis { get; set; }
        CancellationTokenSource TimeoutCancellationHandle { get; set; }
        TaskCompletionSource<IQuasiHttpResponse> SendCallback { get; set; }

        Task CancelAsync(Exception e);
        Task<IQuasiHttpResponse> SendAsync(IQuasiHttpRequest request);
        Task ReceiveAsync();
    }
}
