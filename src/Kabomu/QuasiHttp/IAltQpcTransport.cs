using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IAltQpcTransport : IQpcTransport
    {
        bool ShouldSerialize();
        void ProcessPostRequest(QuasiHttpRequestMessage request, Action<Exception, QuasiHttpResponseMessage> cb);
    }
}
