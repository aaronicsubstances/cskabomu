using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpMiddleware
    {
        void ProcessPostRequest(QuasiHttpRequestMessage request, IQuasiHttpApplication application,
            Action<Exception, object> cb);
    }
}
