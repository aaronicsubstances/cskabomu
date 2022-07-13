using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public interface IQuasiHttpRequest
    {
        string Path { get; }
        IDictionary<string, List<string>> Headers { get; }
        IQuasiHttpBody Body { get; }
        string HttpMethod { get; }
        string HttpVersion { get; }
    }
}
