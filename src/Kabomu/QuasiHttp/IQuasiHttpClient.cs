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
        void Send(QuasiHttpRequestMessage request, object connectionHandleOrRemoteEndpoint, 
            QuasiHttpPostOptions options, Action<Exception, QuasiHttpResponseMessage> cb);
        void ReceivePdu(byte[] data, int offset, int length, object connectionHandle);
        void Reset(Exception cause, Action<Exception> cb);
    }
}
