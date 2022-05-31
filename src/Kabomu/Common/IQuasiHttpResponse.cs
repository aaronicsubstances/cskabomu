using System;
using System.Collections.Generic;
using System.Text;

namespace Kabomu.Common
{
    public interface IQuasiHttpResponse
    {
        bool StatusIndicatesSuccess { get; }
        bool StatusIndicatesClientError { get; }
        string StatusMessage { get; }
        Dictionary<string, List<string>> Headers { get; }
        IQuasiHttpBody Body { get; }
    }
}
