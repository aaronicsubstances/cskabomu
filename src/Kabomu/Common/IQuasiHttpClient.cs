using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpClient
    {
        int DefaultTimeoutMillis { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        Task<IQuasiHttpResponse> SendAsync(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options);
        Task ReceiveAsync(object connection);
        Task ResetAsync(Exception cause);
    }
}
