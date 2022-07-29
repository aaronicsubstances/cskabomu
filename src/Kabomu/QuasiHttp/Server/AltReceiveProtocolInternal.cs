using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class AltReceiveProtocolInternal
    {
        public object Parent { get; set; }
        public Func<object, Exception, IQuasiHttpResponse, Task> AbortCallback { get; set; }
        public IQuasiHttpApplication Application { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }

        public async Task<IQuasiHttpResponse> SendToApplication(IQuasiHttpRequest request)
        {
            if (Application == null)
            {
                throw new MissingDependencyException("application");
            }

            var res = await Application.ProcessRequest(request, RequestEnvironment);

            if (res == null)
            {
                throw new Exception("no response");
            }

            await AbortCallback.Invoke(Parent, null, res);

            return res;
        }
    }
}
