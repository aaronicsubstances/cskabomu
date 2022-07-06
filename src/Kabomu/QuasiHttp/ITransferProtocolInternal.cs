using Kabomu.Common;
using Kabomu.Concurrency;
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
        int MaxChunkSize { get; set; }
        bool IsAborted { get; set; }
        CancellationTokenSource TimeoutCancellationHandle { get; set; }
        Task Cancel();
    }
}
