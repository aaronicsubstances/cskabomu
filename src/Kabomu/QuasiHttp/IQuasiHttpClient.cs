using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpClient
    {
        int DefaultTimeoutMillis { get; set; }
        int MaxRetryPeriodMillis { get; set; }
        int MaxRetryCount { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        void Send(object remoteEndpoint, QuasiHttpRequestMessage request,
            QuasiHttpSendOptions options, Action<Exception, QuasiHttpResponseMessage> cb);
        void OnReceive(object connection);
        void OnReceiveMessage(object connection, byte[] data, int offset, int length);
        void Reset(Exception cause, Action<Exception> cb);
    }
}
