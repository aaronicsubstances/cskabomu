using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpClient
    {
        int DefaultTimeoutMillis { get; set; }
        string LocalEndpoint { get; set; }
        IQuasiHttpApplication Application { get; set; }
        IQuasiHttpTransport Transport { get; set; }
        void BeginPostRequest(QuasiHttpRequestMessage request, QuasiHttpPostOptions options,
            Action<Exception, QuasiHttpResponseMessage> cb);
        void ReceivePdu(byte[] data, int offset, int length, Action<Exception> cb);
        void Reset(Exception cause, Action<Exception> cb);
    }
}
