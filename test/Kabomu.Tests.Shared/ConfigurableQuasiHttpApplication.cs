using Kabomu.QuasiHttp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpApplication : IQuasiHttpApplication
    {
        public Func<IQuasiHttpRequest, IQuasiHttpProcessingOptions, Task<IQuasiHttpResponse>> ProcessRequestCallback { get; set; }

        public Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request, IQuasiHttpProcessingOptions options)
        {
            return ProcessRequestCallback.Invoke(request, options);
        }
    }
}
