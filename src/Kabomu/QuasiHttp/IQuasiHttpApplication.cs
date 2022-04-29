using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpApplication
    {
        void ProcessRequest(QuasiHttpRequestMessage request, Action<Exception, QuasiHttpResponseMessage> cb);
    }
}
