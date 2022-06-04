using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpClient
    {
        int DefaultTimeoutMillis { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        void Send(object remoteEndpoint, IQuasiHttpRequest request,
            IQuasiHttpSendOptions options, Action<Exception, IQuasiHttpResponse> cb);
        void OnReceive(object connection);
        void Reset(Exception cause, Action<Exception> cb);
    }
}
