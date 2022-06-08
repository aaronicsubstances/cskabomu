using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpRequest
    {
        string Path { get; }
        Dictionary<string, List<string>> Headers { get; }
        IQuasiHttpBody Body { get; }
        string HttpMethod { get; }
        string HttpVersion { get; }
    }
}
