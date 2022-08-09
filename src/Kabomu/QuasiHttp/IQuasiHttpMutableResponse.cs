using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpMutableResponse : IQuasiHttpResponse
    {
        new bool StatusIndicatesSuccess { get; set; }

        new bool StatusIndicatesClientError { get;  set; }

        new string StatusMessage { get; set; }

        new IDictionary<string, List<string>> Headers { get; set; }

        new IQuasiHttpBody Body { get; set; }

        new int HttpStatusCode { get; set; }

        new string HttpVersion { get; set; }
    }
}
