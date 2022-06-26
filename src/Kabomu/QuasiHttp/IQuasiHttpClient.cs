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
        IQuasiHttpTransport Transport { get; set; }
        Task<IQuasiHttpResponse> Send(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options);
        Task Reset(Exception cause);
    }
}
