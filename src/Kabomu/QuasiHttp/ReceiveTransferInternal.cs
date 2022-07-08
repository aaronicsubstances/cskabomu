using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveTransferInternal
    {
        public CancellationTokenSource TimeoutCancellationHandle { get; set; }
        public bool IsAborted { get; set; }
        public ReceiveProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<object> CancellationTcs { get; set; }
    }
}