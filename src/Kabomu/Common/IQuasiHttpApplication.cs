using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpApplication
    {
        void ProcessRequest(IQuasiHttpRequestMessage request, Action<Exception, IQuasiHttpResponseMessage> cb);
    }
}
