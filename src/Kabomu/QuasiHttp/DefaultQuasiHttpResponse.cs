using Kabomu.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpResponse : IQuasiHttpResponse
    {
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public Dictionary<string, List<string>> Headers { get; set; }
        public IQuasiHttpBody Body { get; set; }
        public int HttpStatusCode { get; set; }
        public string HttpVersion { get; set; }
        public async Task CloseAsync()
        {
            if (Body != null)
            {
                await Body.EndRead(null);
            }
        }
    }
}
