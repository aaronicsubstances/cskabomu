using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpApplication : IQuasiHttpApplication
    {
        public Action<IQuasiHttpRequest, Action<Exception, IQuasiHttpResponse>> ProcessRequestCallback { get; set; }

        public void ProcessRequest(IQuasiHttpRequest request, Action<Exception, IQuasiHttpResponse> cb)
        {
            ProcessRequestCallback?.Invoke(request, cb);
        }
    }
}
