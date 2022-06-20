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
        Task<IQuasiHttpResponse> Send(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options);
        Task Receive(object connection);
        Task Reset(Exception cause);
    }
}
