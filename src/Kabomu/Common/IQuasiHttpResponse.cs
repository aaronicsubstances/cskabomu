using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.Common
{
    public interface IQuasiHttpResponse
    {
        bool StatusIndicatesSuccess { get; }
        bool StatusIndicatesClientError { get; }
        string StatusMessage { get; }
        Dictionary<string, List<string>> Headers { get; }
        IQuasiHttpBody Body { get; }
        int HttpStatusCode { get; }
        string HttpVersion { get; }
        Task CloseAsync();
    }
}
