using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpClient
    {
        int DefaultTimeoutMillis { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        void Send(QuasiHttpRequestMessage request, object remoteEndpoint, 
            QuasiHttpSendOptions options, Action<Exception, QuasiHttpResponseMessage> cb);
        void ReceivePdu(QuasiHttpPdu pdu, object connectionHandle);
        void Reset(Exception cause, Action<Exception> cb);
    }
}
