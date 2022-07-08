using Kabomu.Concurrency;
using Kabomu.QuasiHttp.Transport;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpClient
    {
        IQuasiHttpSendOptions DefaultSendOptions { get; set; }
        IQuasiHttpClientTransport Transport { get; set; }
        IQuasiHttpTransportBypass TransportBypass { get; set; }
        double TransportBypassProbabilty { get; set; }
        Task<IQuasiHttpResponse> Send(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options);
        Task Reset();
        IMutexApi MutexApi { get; set; }
        IMutexApiFactory MutexApiFactory { get; set; }
    }
}
