using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Tests.Shared
{
    public class ConfigurableQuasiHttpApplication : IQuasiHttpApplication
    {
        public Func<IQuasiHttpRequest, IDictionary<string, object>, Task<IQuasiHttpResponse>> ProcessRequestCallback { get; set; }

        public Task<IQuasiHttpResponse> ProcessRequest(IQuasiHttpRequest request, IDictionary<string, object> requestEnvironment)
        {
            return ProcessRequestCallback.Invoke(request, requestEnvironment);
        }
    }
}
