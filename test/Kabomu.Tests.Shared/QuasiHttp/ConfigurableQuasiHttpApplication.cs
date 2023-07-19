using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared.QuasiHttp
{
    public class ConfigurableQuasiHttpApplication : IQuasiHttpApplication
    {
        public Func<IQuasiHttpRequest, Task<IQuasiHttpResponse>> ProcessRequestCallback { get; set; }

        public Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request)
        {
            return ProcessRequestCallback.Invoke(request);
        }
    }
}
