using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiWsgi
{
    public interface IQuasiHttpContext
    {
        IQuasiHttpRequestMessage Request { get; }
        Dictionary<string, object> RequestAttributes { get; }
        IQuasiHttpResponseMessage Response { get; }
        bool ResponseMarkedAsSent { get; }
        Exception Error { get; }
    }
}
