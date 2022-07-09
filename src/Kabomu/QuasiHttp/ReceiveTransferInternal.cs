using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveTransferInternal
    {
        public CancellationTokenSource TransferCancellationHandle { get; set; }
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public ReceiveProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<object> CancellationTcs { get; set; }
    }
}