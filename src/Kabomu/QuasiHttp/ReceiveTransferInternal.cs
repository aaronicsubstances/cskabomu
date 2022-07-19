using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    internal class ReceiveTransferInternal
    {
        public object TimeoutId { get; set; }
        public bool IsAborted { get; set; }
        public ReceiveProtocolInternal Protocol { get; set; }
        public TaskCompletionSource<object> CancellationTcs { get; set; }
    }
}