using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class AltReceiveProtocolInternal: IReceiveProtocolInternal
    {
        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpRequest Request { get; set; }
        public IDictionary<string, object> RequestEnvironment { get; set; }

        public void Cancel()
        {
            Application = null;
            Request = null;
            RequestEnvironment = null;
        }

        public async Task<IQuasiHttpResponse> Receive()
        {
            if (Application == null)
            {
                throw new MissingDependencyException("application");
            }
            if (Request == null)
            {
                throw new MissingDependencyException("request");
            }

            var res = await Application.ProcessRequest(Request, RequestEnvironment);

            if (res == null)
            {
                throw new ExpectationViolationException("no response");
            }

            return res;
        }
    }
}
