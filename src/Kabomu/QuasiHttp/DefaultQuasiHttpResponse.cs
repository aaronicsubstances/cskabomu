using Kabomu.QuasiHttp.EntityBody;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kabomu.QuasiHttp
{
    public class DefaultQuasiHttpResponse : IQuasiHttpResponse
    {
        public bool StatusIndicatesSuccess { get; set; }
        public bool StatusIndicatesClientError { get; set; }
        public string StatusMessage { get; set; }
        public IDictionary<string, List<string>> Headers { get; set; }
        public IQuasiHttpBody Body { get; set; }
        public int HttpStatusCode { get; set; }
        public string HttpVersion { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public Task Close()
        {
            CancellationTokenSource?.Cancel();
            var endReadTask = Body?.EndRead();
            return endReadTask ?? Task.CompletedTask;
        }
    }
}
