using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Client
{
    public interface IQuasiHttpClient
    {
        IQuasiHttpSendOptions DefaultSendOptions { get; set; }
        IQuasiHttpClientTransport Transport { get; set; }
        IQuasiHttpAltTransport TransportBypass { get; set; }
        double TransportBypassProbabilty { get; set; }
        double ResponseStreamingProbabilty { get; set; }
        IMutexApi MutexApi { get; set; }
        ITimerApi TimerApi { get; set; }
        Task Reset(Exception cause);
        Task<IQuasiHttpResponse> Send(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options);
        Tuple<Task<IQuasiHttpResponse>, object> Send2(object remoteEndpoint,
            IQuasiHttpRequest request, IQuasiHttpSendOptions options);
        void CancelSend(object sendCancellationHandle);
    }
}
