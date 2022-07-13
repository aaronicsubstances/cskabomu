using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpResponse
    {
        bool StatusIndicatesSuccess { get; }
        bool StatusIndicatesClientError { get; }
        string StatusMessage { get; }
        IDictionary<string, List<string>> Headers { get; }
        IQuasiHttpBody Body { get; }
        int HttpStatusCode { get; }
        string HttpVersion { get; }
    }
}
