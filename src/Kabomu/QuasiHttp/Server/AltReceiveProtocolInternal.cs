using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp.Server
{
    internal class AltReceiveProtocolInternal: IReceiveProtocolInternal
    {
        public AltReceiveProtocolInternal()
        {
            
        }

        public IQuasiHttpApplication Application { get; set; }
        public IQuasiHttpRequest Request { get; set; }

        public Task Cancel()
        {
            return Task.CompletedTask;
        }

        public Task<IQuasiHttpResponse> Receive()
        {
            if (Application == null)
            {
                throw new MissingDependencyException("application");
            }
            if (Request == null)
            {
                throw new ExpectationViolationException("request");
            }

            return Application.ProcessRequest(Request);
        }
    }
}
